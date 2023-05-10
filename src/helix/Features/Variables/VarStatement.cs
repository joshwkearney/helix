﻿using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Variables;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.Aggregates;
using Helix.Features.Primitives;
using Helix.Analysis.Lifetimes;
using Helix.Features.FlowControl;
using Helix.Features.Memory;
using System.IO;
using helix.FlowAnalysis;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree VarExpression() {
            TokenLocation startLok;
            bool isWritable;

            if (this.Peek(TokenKind.VarKeyword)) {
                startLok = this.Advance(TokenKind.VarKeyword).Location;
                isWritable = true;
            }
            else {
                startLok = this.Advance(TokenKind.LetKeyword).Location;
                isWritable = false;
            }

            var names = new List<string>();

            while (true) {
                var name = this.Advance(TokenKind.Identifier).Value;
                names.Add(name);

                if (this.TryAdvance(TokenKind.Assignment)) {
                    break;
                }
                else { 
                    this.Advance(TokenKind.Comma);
                }
            }

            var assign = this.TopExpression();
            var loc = startLok.Span(assign.Location);
            var result = new VarParseStatement(loc, names, assign, isWritable);

            return result;
        }
    }
}

namespace Helix {
    public record VarParseStatement : ISyntaxTree {
        private readonly IReadOnlyList<string> names;
        private readonly ISyntaxTree assign;
        private readonly bool isWritable;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.assign };

        public bool IsPure => false;

        public VarParseStatement(TokenLocation loc, IReadOnlyList<string> names, ISyntaxTree assign, bool isWritable) {
            this.Location = loc;
            this.names = names;
            this.assign = assign;
            this.isWritable = isWritable;
        }

        public VarParseStatement(TokenLocation loc, string name, ISyntaxTree assign, bool isWritable)
            : this(loc, new[] { name }, assign, isWritable) { }

        public ISyntaxTree CheckTypes(EvalFrame types) {
            // Type check the assignment value
            var assign = this.assign.CheckTypes(types).ToRValue(types);
            if (this.isWritable) {
                assign = assign.WithMutableType(types);
            }

            // If this is a compound assignment, check if we have the right
            // number of names and then recurse
            var assignType = types.ReturnTypes[assign];
            if (this.names.Count > 1) {
                return this.Destructure(assignType, types);
            }

            // Make sure we're not shadowing another variable
            var basePath = this.Location.Scope.Append(this.names[0]);
            if (types.Variables.ContainsKey(basePath)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.names[0]);
            }

            // Go through all the variables and sub variables and set up the lifetimes
            // correctly
            foreach (var (compPath, compType) in VariablesHelper.GetMemberPaths(assignType, types)) {
                var path = basePath.Append(compPath);
                var sig = new VariableSignature(path, compType, this.isWritable);

                // Add this variable's lifetime
                types.Variables[path] = sig;
            }

            // Put this variable's value in the main table
            types.SyntaxValues[basePath] = assign;

            // Set the return type of the new syntax tree
            var result = new VarStatement(this.Location, basePath, assignType, assign);
            types.ReturnTypes[result] = PrimitiveType.Void;

            return result.CheckTypes(types);
        }

        private ISyntaxTree Destructure(HelixType assignType, EvalFrame types) {
            if (assignType is not NamedType named) {
                throw new TypeCheckingException(
                    this.Location,
                    "Invalid Desconstruction",
                    $"Cannot deconstruct non-struct type '{ assignType }'");
            }

            if (!types.Structs.TryGetValue(named.Path, out var sig)) {
                throw new TypeCheckingException(
                    this.Location,
                    "Invalid Desconstruction",
                    $"Cannot deconstruct non-struct type '{assignType}'");
            }

            if (sig.Members.Count != this.names.Count) {
                throw new TypeCheckingException(
                    this.Location,
                    "Invalid Desconstruction",
                    "The number of variables provided does not match " 
                        + $"the number of members on struct type '{named}'");
            }

            var tempName = types.GetVariableName();
            var tempStat = new VarParseStatement(
                this.Location,
                new[] { tempName },
                this.assign,
                false);

            var stats = new List<ISyntaxTree>() { tempStat };

            for (int i = 0; i < sig.Members.Count; i++) {
                var literal = new VariableAccessParseSyntax(this.Location, tempName);
                var access = new MemberAccessSyntax(this.Location, literal, sig.Members[i].Name);
                var assign = new VarParseStatement(
                    this.Location,
                    new[] { this.names[i] },
                    access,
                    this.isWritable);

                stats.Add(assign);
            }

            return new CompoundSyntax(this.Location, stats).CheckTypes(types);
        }
    }

    public record VarStatement : ISyntaxTree {
        private readonly ISyntaxTree assign;
        private readonly IdentifierPath path;
        private readonly HelixType returnType;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.assign };

        public bool IsPure => false;

        public VarStatement(TokenLocation loc, IdentifierPath path, HelixType returnType, ISyntaxTree assign) {
            this.Location = loc;
            this.path = path;
            this.assign = assign;
            this.returnType = returnType;
        }

        public ISyntaxTree CheckTypes(EvalFrame types) => this;

        public ISyntaxTree ToRValue(EvalFrame types) => this;

        public void AnalyzeFlow(FlowFrame flow) {
            this.assign.AnalyzeFlow(flow);

            // Calculate a signature and lifetime for this variable
            var assignType = this.assign.GetReturnType(flow);
            var assignBundle = this.assign.GetLifetimes(flow);

            // Go through all the variables and sub variables and set up the lifetimes
            // correctly
            foreach (var (compPath, compType) in assignType.GetMembers(flow)) {
                var path = this.path.Append(compPath);
                var varLifetime = new Lifetime(path, 0);

                // Add this variable members's lifetime
                flow.VariableLifetimes[path] = varLifetime;

                // Make sure that this variable acts as a passthrough for the lifetimes that are
                // in the assignment expression
                if (!compType.IsValueType(flow)) {
                    flow.LifetimeGraph.AddAlias(varLifetime, assignBundle.Components[compPath]);
                }

                // TODO: Put back binding
                //if (sig.Type.IsRemote(flow)) {
                    //bindings.Add(new BindLifetimeSyntax(this.Location, varLifetime, path));
                //}
            }

            this.SetLifetimes(new LifetimeBundle(), flow);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            var name = writer.GetVariableName(this.path);

            var stat = new CVariableDeclaration() {
                Type = writer.ConvertType(returnType),
                Name = name,
                Assignment = Option.Some(this.assign.GenerateCode(types, writer))
            };

            foreach (var (relPath, _) in VariablesHelper.GetMemberPaths(this.returnType, types)) {
                writer.SetMemberPath(this.path, relPath);
            }

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: New variable declaration '{this.path.Segments.Last()}'");
            writer.WriteStatement(stat);
            writer.WriteEmptyLine();

            return new CIntLiteral(0);
        }
    }
}