using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Variables;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.Aggregates;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Features.FlowControl;
using System.Xml.Linq;

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

            return new VarParseStatement(loc, names, assign, isWritable);
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

            // Make sure we're not shadowing anybody
            if (types.TryResolveName(this.Location.Scope, this.names[0], out _)) {
                throw TypeException.IdentifierDefined(this.Location, this.names[0]);
            }

            var basePath = this.Location.Scope.Append(this.names[0]);

            // Declare all our stuff
            types.DeclareVariableSignatures(basePath, assignType, this.isWritable);
            types.DeclareLocationLifetimeRoots(basePath, assignType, LifetimeRole.Inference);
            types.DeclareValueLifetimeRoots(basePath, assignType, LifetimeRole.Inference);

            // Put this variable's value in the main table
            types.SyntaxValues[basePath] = assign;

            var result = new VarStatement(
                this.Location, 
                basePath, 
                assign, 
                types.LifetimeRoots.ToHashSet());

            result.SetReturnType(PrimitiveType.Void, types);

            return result;
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
            var tempStat = new VarParseStatement(
                this.Location,
                new[] { tempName },
                this.assign,
                false);

            var stats = new List<ISyntaxTree>() { tempStat };

            for (int i = 0; i < sig.Members.Count; i++) {
                var literal = new VariableAccessParseSyntax(this.Location, tempName);
                var access = new MemberAccessSyntax(this.Location, literal, sig.Members[i].Name, this.isWritable);

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
        private readonly ISyntaxTree assignSyntax;
        private readonly IdentifierPath path;
        private readonly IReadOnlySet<Lifetime> allowedRoots;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.assignSyntax };

        public bool IsPure => false;

        public VarStatement(TokenLocation loc, IdentifierPath path,
                            ISyntaxTree assign, IReadOnlySet<Lifetime> allowedRoots) {
            this.Location = loc;
            this.path = path;
            this.assignSyntax = assign;
            this.allowedRoots = allowedRoots;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) => this;

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public void AnalyzeFlow(FlowFrame flow) {
            this.assignSyntax.AnalyzeFlow(flow);

            var assignType = this.assignSyntax.GetReturnType(flow);
            var assignBundle = this.assignSyntax.GetLifetimes(flow);

            flow.DeclareLocationLifetimes(this.path, assignType, LifetimeRole.Inference);
            flow.DeclareValueLifetimes(this.path, assignType, assignBundle, LifetimeRole.Inference);

            this.SetLifetimes(new LifetimeBundle(), flow);
        }

        public ICSyntax GenerateCode(FlowFrame flow, ICStatementWriter writer) {
            var basePath = this.path.ToVariablePath();
            var roots = flow.GetRoots(flow.LocationLifetimes[basePath]).ToValueList();

            if (roots.Any() && roots.Any(x => !this.allowedRoots.Contains(x))) {
                throw new LifetimeException(
                    this.Location,
                    "Lifetime Inference Failed",
                    "The lifetime of this new object allocation has failed because it is " +
                    "dependent on a root that does not exist at this point in the program and " +
                    "must be calculated at runtime. Please try moving the allocation " +
                    "closer to the site of its use.");
            }

            var assign = this.assignSyntax.GenerateCode(flow, writer);
            var allocLifetime = writer.CalculateSmallestLifetime(this.Location, roots);

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: New variable declaration '{this.path.Segments.Last()}'");

            if (roots.Count == 0) {
                this.GenerateStackAllocation(assign, flow, writer);
            }
            else {
                this.GenerateRegionAllocation(assign, allocLifetime, flow, writer);
            }

            return new CIntLiteral(0);
        }

        private void GenerateStackAllocation(ICSyntax assign, FlowFrame flow, ICStatementWriter writer) {
            var name = writer.GetVariableName(this.path);
            var assignType = this.assignSyntax.GetReturnType(flow);
            var cReturnType = writer.ConvertType(assignType);

            var stat = new CVariableDeclaration() {
                Type = cReturnType,
                Name = name,
                Assignment = Option.Some(assign)
            };

            writer.WriteStatement(stat);
            writer.WriteEmptyLine();
            writer.RegisterVariableKind(this.path, CVariableKind.Local);

            // Register all of our value lifetimes. We don't need to register any location 
            // lifetimes because they don't exist, as we are stack allocated
            writer.RegisterLocationLifetimes(this.path, assignType, new CVariableLiteral("get_smallest_lifetime()"), flow);
            writer.RegisterValueLifetimes(this.path, assignType, assign, flow);
        }

        private void GenerateRegionAllocation(ICSyntax assign, ICSyntax allocLifetime, 
                                              FlowFrame flow, ICStatementWriter writer) {
            var name = writer.GetVariableName(this.path);
            var assignType = this.assignSyntax.GetReturnType(flow);
            var cReturnType = writer.ConvertType(assignType);

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

            // Register all our lifetimes
            writer.RegisterLocationLifetimes(this.path, assignType, allocLifetime, flow);
            writer.RegisterValueLifetimes(this.path, assignType, assign, flow);
        }
    }
}