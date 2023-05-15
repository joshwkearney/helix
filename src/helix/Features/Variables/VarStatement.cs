using Helix.Analysis;
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
            var result = new VarStatement(this.Location, basePath, assignType, assign, types.LifetimeRoots.Values.ToHashSet());
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
        private readonly IReadOnlySet<Lifetime> allowedRoots;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.assign };

        public bool IsPure => false;

        public VarStatement(TokenLocation loc, IdentifierPath path, HelixType returnType, 
                            ISyntaxTree assign, IReadOnlySet<Lifetime> allowedRoots) {
            this.Location = loc;
            this.path = path;
            this.assign = assign;
            this.returnType = returnType;
            this.allowedRoots = allowedRoots;
        }

        public ISyntaxTree CheckTypes(EvalFrame types) => this;

        public ISyntaxTree ToRValue(EvalFrame types) => this;

        public void AnalyzeFlow(FlowFrame flow) {
            this.assign.AnalyzeFlow(flow);

            // Calculate a signature and lifetime for this variable
            var assignType = this.assign.GetReturnType(flow);
            var assignBundle = this.assign.GetLifetimes(flow);
            var baseLifetime = new Lifetime(this.path, 0, LifetimeKind.Inferencee);

            // Go through all the variables and sub variables and set up the lifetimes
            // correctly
            foreach (var (relPath, _) in assignType.GetMembers(flow)) {
                var path = this.path.Append(relPath);
                var varLifetime = new Lifetime(path, 0, LifetimeKind.Inferencee);

                // Add a dependency between this version of the variable lifetime
                // and the assigned expression. Whenever an alias might occur the
                // version will be incremented, so this will not be unsafe with
                // mutable variables
                flow.LifetimeGraph.RequireOutlives(
                    assignBundle.Components[relPath],
                    varLifetime);

                // Make sure we say the main lifetime outlives all of the member lifetimes
                flow.LifetimeGraph.RequireOutlives(baseLifetime, varLifetime);

                // Add this variable members's lifetime
                flow.VariableValueLifetimes[path] = assignBundle.Components[relPath];
            }

            this.SetLifetimes(new LifetimeBundle(), flow);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            var roots = types
                .LifetimeGraph
                .GetOutlivedLifetimes(new Lifetime(this.path, 0, LifetimeKind.Inferencee))
                .Where(x => x.Kind != LifetimeKind.Inferencee)
                .ToValueList();

            // This removes redundant roots that are outlived by other roots
            // We only need to allocate on the longest-lived of our roots
            roots = types.ReduceRootSet(roots).ToValueList();

            if (roots.Any() && roots.Any(x => !this.allowedRoots.Contains(x))) {
                throw new LifetimeException(
                    this.Location,
                    "Lifetime Inference Failed",
                    "The lifetime of this new object allocation has failed because it is " +
                    "dependent on a root that does not exist at this point in the program and " +
                    "must be calculated at runtime. Please try moving the allocation " +
                    "closer to the site of its use.");
            }

            foreach (var (relPath, _) in VariablesHelper.GetMemberPaths(this.returnType, types)) {
                writer.RegisterMemberPath(this.path, relPath);
            }

            var isStack = roots.Count == 0 || (roots.Count == 1 && roots[0] == Lifetime.Stack);
            var name = writer.GetVariableName(this.path);
            var assign = this.assign.GenerateCode(types, writer);
            var cReturnType = writer.ConvertType(returnType);

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: New variable declaration '{this.path.Segments.Last()}'");

            if (isStack) {
                var stat = new CVariableDeclaration() {
                    Type = cReturnType,
                    Name = name,
                    Assignment = Option.Some(assign)
                };

                writer.WriteStatement(stat);
                writer.WriteEmptyLine();
                writer.RegisterVariableKind(this.path, CVariableKind.Local);
            }
            else {
                // Allocate on the heap
                var allocLifetime = writer.CalculateSmallestLifetime(roots.Where(x => x != Lifetime.Stack).ToValueList());

                writer.WriteStatement(new CVariableDeclaration() {
                    Name = name,
                    Type = new CPointerType(cReturnType),
                    Assignment = new CVariableLiteral($"({cReturnType.WriteToC()}*)_region_malloc({allocLifetime.WriteToC()}, sizeof({cReturnType.WriteToC()}))")
                });

                var assignmentDecl = new CAssignment() {
                    Left = new CPointerDereference() {
                        Target = new CVariableLiteral(name)
                    },
                    Right = assign
                };

                writer.WriteStatement(assignmentDecl);
                writer.WriteEmptyLine();
                writer.RegisterVariableKind(this.path, CVariableKind.Allocated);
            }

            return new CIntLiteral(0);
        }
    }
}