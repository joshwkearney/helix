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
using Helix.Collections;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree VarExpression() {
            var startLok = this.Advance(TokenKind.VarKeyword).Location;
            var names = new List<string>();
            var types = new List<Option<ISyntaxTree>>();

            while (true) {
                var name = this.Advance(TokenKind.Identifier).Value;
                names.Add(name);

                if (this.TryAdvance(TokenKind.AsKeyword)) {
                    types.Add(Option.Some(this.TopExpression()));
                }
                else {
                    types.Add(Option.None);
                }

                if (this.TryAdvance(TokenKind.Assignment)) {
                    break;
                }
                else {
                    this.Advance(TokenKind.Comma);
                }
            }

            var assign = this.TopExpression();
            var loc = startLok.Span(assign.Location);

            return new VarParseStatement(loc, names, types, assign);
        }
    }
}

namespace Helix {
    public record VarParseStatement : ISyntaxTree {
        private readonly IReadOnlyList<string> names; 
        private readonly IReadOnlyList<Option<ISyntaxTree>> types;
        private readonly ISyntaxTree assign;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.assign };

        public bool IsPure => false;

        public VarParseStatement(TokenLocation loc, IReadOnlyList<string> names, 
                                 IReadOnlyList<Option<ISyntaxTree>> types, ISyntaxTree assign) {
            this.Location = loc;
            this.names = names;
            this.assign = assign;
            this.types = types;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            // Type check the assignment value
            var assign = this.assign.CheckTypes(types).ToRValue(types);

            // If this is a compound assignment, check if we have the right
            // number of names and then recurse
            var assignType = assign.GetReturnType(types);
            if (this.names.Count > 1) {
                return this.Destructure(assignType, types);
            }

            // Make sure assign can unify with our type expression
            if (this.types[0].TryGetValue(out var typeSyntax)) {
                if (!typeSyntax.AsType(types).TryGetValue(out var type)) {
                    throw TypeException.ExpectedTypeExpression(typeSyntax.Location);
                }

                assign = assign.UnifyTo(type, types);
                assignType = type;
            }

            // Make sure we're not shadowing anybody
            if (types.TryResolveName(types.Scope, this.names[0], out _)) {
                throw TypeException.IdentifierDefined(this.Location, this.names[0]);
            }

            var path = types.Scope.Append(this.names[0]);
            var varSig = new PointerType(assignType);

            types.NominalSignatures.Add(path, varSig);

            var result = new VarStatement(
                this.Location,
                path,
                assign);

            SyntaxTagBuilder.AtFrame(types)
                .WithLifetimes(AnalyzeFlow(this.Location, assign, path, types))
                .BuildFor(result);

            return result.CheckTypes(types);
        }

        private static LifetimeBounds AnalyzeFlow(TokenLocation loc, ISyntaxTree assign, 
                                                  IdentifierPath path, TypeFrame flow) {
            var assignBounds = assign.GetLifetimes(flow);
            var allowedRoots = flow.ValidRoots.ToValueSet();

            var locationLifetime = new InferredLocationLifetime(
                loc,
                path,
                allowedRoots,
                LifetimeOrigin.LocalLocation);

            var valueLifetime = new ValueLifetime(path, LifetimeRole.Alias, LifetimeOrigin.LocalValue);

            // Add a dependency between whatever is being assigned to this variable and the
            // variable's value
            flow.DataFlow.AddAssignment(
                assignBounds.ValueLifetime,
                valueLifetime,
                assign.GetReturnType(flow));

            // The value of a variable must outlive its location
            flow.DataFlow.AddStored(
                valueLifetime, 
                locationLifetime, 
                assign.GetReturnType(flow));

            // Add this variable lifetimes to the current frame
            var bounds = new LifetimeBounds(valueLifetime, locationLifetime);
            var named = new NominalType(path, NominalTypeKind.Variable);
            var local = new LocalInfo(named, bounds);

            flow.Locals = flow.Locals.Add(path, local);

            // TODO: Fix this
            // HACK: Even though we're returning void, set the lifetime of this syntax
            // to be the lifetime of our variable. This gets around the issue of variable
            // lifetimes being stored per-flow frame and means that we don't have to 
            // regenerate the syntax tree on flow analysis (very good)
            return bounds;
        }

        private ISyntaxTree Destructure(HelixType assignType, TypeFrame types) {
            if (!assignType.AsStruct(types).TryGetValue(out var sig)) {
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
                        + $"the number of members on struct type '{assignType}'");
            }

            var tempName = types.GetVariableName();
            var tempStat = new VarParseStatement(
                this.Location,
                new[] { tempName },
                new Option<ISyntaxTree>[] { Option.None },
                this.assign);

            var stats = new List<ISyntaxTree>() { tempStat };

            for (int i = 0; i < sig.Members.Count; i++) {
                var literal = new VariableAccessParseSyntax(this.Location, tempName);
                var access = new MemberAccessSyntax(this.Location, literal, sig.Members[i].Name, types.Scope);

                var assign = new VarParseStatement(
                    this.Location,
                    new[] { this.names[i] },
                    new[] { this.types[i] },
                    access);

                stats.Add(assign);
            }

            return new CompoundSyntax(this.Location, stats).CheckTypes(types);
        }
    }

    public record VarStatement : ISyntaxTree {
        private readonly ISyntaxTree assignSyntax;
        private readonly IdentifierPath path;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.assignSyntax };

        public bool IsPure => false;

        public VarStatement(TokenLocation loc, IdentifierPath path, ISyntaxTree assign) {
            this.Location = loc;
            this.path = path;
            this.assignSyntax = assign;
        }

        public Option<HelixType> AsType(TypeFrame types) {
            return new NominalType(this.path, NominalTypeKind.Variable);
        }

        public ISyntaxTree CheckTypes(TypeFrame types) => this;

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public ICSyntax GenerateCode(TypeFrame flow, ICStatementWriter writer) {
            var assign = this.assignSyntax.GenerateCode(flow, writer);
            var lifetime = this.GetLifetimes(flow).LocationLifetime;
            var allocLifetime = lifetime.GenerateCode(flow, writer);

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: New variable declaration '{this.path.Segments.Last()}'");

            if (flow.GetMaximumRoots(lifetime).Any()) {
                this.GenerateRegionAllocation(assign, allocLifetime, flow, writer);
            }
            else {
                this.GenerateStackAllocation(assign, flow, writer);
            }

            return new CIntLiteral(0);
        }

        private void GenerateStackAllocation(ICSyntax assign, TypeFrame types, ICStatementWriter writer) {
            var name = writer.GetVariableName(this.path);
            var assignType = this.assignSyntax.GetReturnType(types);
            var cReturnType = writer.ConvertType(assignType, types);

            var stat = new CVariableDeclaration() {
                Type = cReturnType,
                Name = name,
                Assignment = Option.Some(assign)
            };

            writer.WriteStatement(stat);
            writer.WriteEmptyLine();
            writer.VariableKinds[this.path] = CVariableKind.Local;
        }

        private void GenerateRegionAllocation(ICSyntax assign, ICSyntax allocLifetime,
                                              TypeFrame types, ICStatementWriter writer) {
            var name = writer.GetVariableName(this.path);
            var assignType = this.assignSyntax.GetReturnType(types);
            var cReturnType = writer.ConvertType(assignType, types);

            writer.WriteStatement(new CVariableDeclaration() {
                Name = name,
                Type = new CPointerType(cReturnType),
                Assignment = new CRegionAllocExpression() {
                    Type = cReturnType,
                    Lifetime = allocLifetime
                }
            });

            var assignmentDecl = new CAssignment() {
                Left = new CPointerDereference() {
                    Target = new CVariableLiteral(name)
                },
                Right = assign
            };

            writer.WriteStatement(assignmentDecl);
            writer.WriteEmptyLine();
            writer.VariableKinds[this.path] = CVariableKind.Allocated;
        }
    }
}