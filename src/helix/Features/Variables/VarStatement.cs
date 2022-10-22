﻿using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Variables;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.Aggregates;
using Helix.Features.Primitives;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree VarExpression(BlockBuilder block) {
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

            var assign = this.TopExpression(block);

            var loc = startLok.Span(assign.Location);
            var result = new VarParseStatement(loc, names, assign, isWritable);

            block.Statements.Add(result);
            return new VoidLiteral(loc);
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

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            // Type check the assignment value
            var assign = this.assign.CheckTypes(types).ToRValue(types);

            if (this.isWritable) {
                assign = assign.WithMutableType(types);
            }

            var assignType = types.ReturnTypes[assign];

            // If this is a compound assignment, check if we have the right
            // number of names and then recurse
            if (this.names.Count > 1) {
                return this.Destructure(assignType, types);
            }

            var path = this.Location.Scope.Append(this.names[0]);
            var sig = new VariableSignature(path, assignType, this.isWritable);

            // Declare this variable and make sure we're not shadowing another variable
            if (types.Variables.ContainsKey(path)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.names[0]);
            }

            // Put this variable's value in the value table
            types.Variables[path] = sig;
            types.Trees[path] = assign;

            // Set the return type of the new syntax tree
            var result = new VarStatement(this.Location, sig, assign);
            types.ReturnTypes[result] = PrimitiveType.Void;
            //types.SetReturnType(result, new PointerType(assignType, this.isWritable));

            return result;
        }

        private ISyntaxTree Destructure(HelixType assignType, SyntaxFrame types) {
            if (assignType is not NamedType named) {
                throw new TypeCheckingException(
                    this.Location,
                    "Invalid Desconstruction",
                    $"Cannot deconstruct non-struct type '{ assignType }'");
            }

            if (!types.Aggregates.TryGetValue(named.Path, out var sig)) {
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

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ISyntaxTree ToLValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }

    public record VarStatement : ISyntaxTree {
        private readonly Option<ISyntaxTree> assign;
        private readonly VariableSignature signature;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => this.assign.ToEnumerable();

        public bool IsPure => false;

        public VarStatement(TokenLocation loc, VariableSignature sig, ISyntaxTree assign) {
            this.Location = loc;
            this.signature = sig;
            this.assign = Option.Some(assign);
        }

        public VarStatement(TokenLocation loc, VariableSignature sig) {
            this.Location = loc;
            this.signature = sig;
            this.assign = Option.None;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) => this;

        public ISyntaxTree ToRValue(SyntaxFrame types) => this;

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            var stat = new CVariableDeclaration() {
                Type = writer.ConvertType(this.signature.Type),
                Name = writer.GetVariableName(this.signature.Path),
                Assignment = this.assign.Select(x => x.GenerateCode(types, writer))
            };

            writer.WriteStatement(stat);
            return new CIntLiteral(0);
        }
    }
}