using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Variables;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.Aggregates;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Syntax.Decorators;
using Helix.Analysis.TypeChecking;

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

            return VarParseStatement.Create(loc, names, assign, isWritable);
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

        private VarParseStatement(TokenLocation loc, IReadOnlyList<string> names, ISyntaxTree assign, bool isWritable) {
            this.Location = loc;
            this.names = names;
            this.assign = assign;
            this.isWritable = isWritable;
        }

        public static ISyntaxTree Create(TokenLocation loc, IReadOnlyList<string> names, ISyntaxTree assign, bool isWritable) {
            return new VarParseStatement(loc, names, assign, isWritable)
                .Decorate(new ShadowingPreventer(names));
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
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

            // Go through all the variables and sub variables and set up the lifetimes
            // correctly
            var basePath = this.Location.Scope.Append(this.names[0]);
            foreach (var (compPath, compType) in assignType.GetMembers(types)) {
                var path = basePath.Append(compPath);
                var sig = new VariableSignature(path, compType, this.isWritable);

                // Add this variable's lifetime
                types.Variables[path] = sig;
            }

            // Put this variable's value in the main table
            types.SyntaxValues[basePath] = assign;

            ISyntaxTree result = new VarStatement(
                this.Location, 
                basePath, 
                assignType, 
                assign, 
                types.LifetimeRoots.Values.ToHashSet());

            result.SetReturnType(PrimitiveType.Void, types);

            return result
                .Decorate(new AssignedLifetimeProducer(basePath, assignType, LifetimeKind.Inferencee, assign))
                .CheckTypes(types);
        }

        private ISyntaxTree Destructure(HelixType assignType, TypeFrame types) {
            if (assignType is not NamedType named) {
                throw new TypeException(
                    this.Location,
                    "Invalid Desconstruction",
                    $"Cannot deconstruct non-struct type '{ assignType }'");
            }

            if (!types.Structs.TryGetValue(named.Path, out var sig)) {
                throw new TypeException(
                    this.Location,
                    "Invalid Desconstruction",
                    $"Cannot deconstruct non-struct type '{assignType}'");
            }

            if (sig.Members.Count != this.names.Count) {
                throw new TypeException(
                    this.Location,
                    "Invalid Desconstruction",
                    "The number of variables provided does not match " 
                        + $"the number of members on struct type '{named}'");
            }

            var tempName = types.GetVariableName();
            var tempStat = VarParseStatement.Create(
                this.Location,
                new[] { tempName },
                this.assign,
                false);

            var stats = new List<ISyntaxTree>() { tempStat };

            for (int i = 0; i < sig.Members.Count; i++) {
                var literal = new VariableAccessParseSyntax(this.Location, tempName);
                var access = new MemberAccessSyntax(this.Location, literal, sig.Members[i].Name, this.isWritable);

                var assign = VarParseStatement.Create(
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
        private readonly HelixType varType;
        private readonly IReadOnlySet<Lifetime> allowedRoots;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.assign };

        public bool IsPure => false;

        public VarStatement(TokenLocation loc, IdentifierPath path, HelixType varType, 
                            ISyntaxTree assign, IReadOnlySet<Lifetime> allowedRoots) {
            this.Location = loc;
            this.path = path;
            this.assign = assign;
            this.varType = varType;
            this.allowedRoots = allowedRoots;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) => this;

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public void AnalyzeFlow(FlowFrame flow) {
            this.assign.AnalyzeFlow(flow);

            this.SetLifetimes(new LifetimeBundle(), flow);
        }

        public ICSyntax GenerateCode(FlowFrame flow, ICStatementWriter writer) {
            var roots = flow
                .LifetimeGraph
                .GetOutlivedLifetimes(flow.VariableLifetimes[this.path])
                .Where(x => !x.Path.StartsWith(this.path))
                //.Where(x => x.Kind != LifetimeKind.Inferencee)
                //.Where(x => x != Lifetime.Stack)
                .ToValueList();

            // This removes redundant roots that are outlived by other roots
            // We only need to allocate on the longest-lived of our roots
            roots = flow.ReduceRootSet(roots).ToValueList();

            if (roots.Any() && roots.Any(x => !this.allowedRoots.Contains(x))) {
                throw new LifetimeException(
                    this.Location,
                    "Lifetime Inference Failed",
                    "The lifetime of this new object allocation has failed because it is " +
                    "dependent on a root that does not exist at this point in the program and " +
                    "must be calculated at runtime. Please try moving the allocation " +
                    "closer to the site of its use.");
            }

            var isStack = roots.Count == 0 || (roots.Count == 1 && roots[0] == Lifetime.Stack);
            var name = writer.GetVariableName(this.path);
            var assign = this.assign.GenerateCode(flow, writer);
            var cReturnType = writer.ConvertType(varType);

            var allocLifetime = writer.CalculateSmallestLifetime(this.Location, roots);

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: New variable declaration '{this.path.Segments.Last()}'");

            if (isStack) {
                // TODO: Register lifetimes here too. Need to fix CalculateSmallestLifetime()

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
                //this.producer.RegisterLifetimes(flow, writer, allocLifetime);

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