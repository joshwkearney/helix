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
            var varSig = new PointerType(assignType, this.isWritable);

            types.NominalSignatures = types.NominalSignatures.SetItem(basePath, varSig);

            var result = new VarStatement(
                this.Location,
                basePath,
                assign,
                this.isWritable);

            // Put this variable's value in the main table
            types.SyntaxValues = types.SyntaxValues.SetItem(basePath, result);

            result.SetReturnType(PrimitiveType.Void, types);
            result.SetCapturedVariables(assign, types);
            result.SetPredicate(assign, types);

            return result.CheckTypes(types);
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
        private readonly bool isWritable;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.assignSyntax };

        public bool IsPure => false;

        public VarStatement(TokenLocation loc, IdentifierPath path, ISyntaxTree assign, bool isWritable) {
            this.Location = loc;
            this.path = path;
            this.assignSyntax = assign;
            this.isWritable = isWritable;
        }

        public Option<HelixType> AsType(TypeFrame types) {
            return new PointerType(this.assignSyntax.GetReturnType(types), this.isWritable);
        }

        public ISyntaxTree CheckTypes(TypeFrame types) => this;

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public void AnalyzeFlow(FlowFrame flow) {
            this.assignSyntax.AnalyzeFlow(flow);

            var bundle = DeclareValueLifetimes(flow);

            // TODO: Fix this
            // HACK: Even though we're returning void, set the lifetime of this syntax
            // to be the lifetime of our variable. This gets around the issue of variable
            // lifetimes being stored per-flow frame and means that we don't have to 
            // regenerate the syntax tree on flow analysis (very good)
            this.SetLifetimes(bundle, flow);
        }

        private LifetimeBundle DeclareValueLifetimes(FlowFrame flow) {

            var assignBundle = this.assignSyntax.GetLifetimes(flow);
            var allowedRoots = flow.LifetimeRoots.ToValueSet();

            var dict = new Dictionary<IdentifierPath, LifetimeBounds>();

            var varLocation = new InferredLocationLifetime(
                this.Location, 
                this.path.ToVariablePath(), 
                allowedRoots, 
                LifetimeOrigin.LocalLocation);

            foreach (var (relPath, memType) in this.assignSyntax.GetReturnType(flow).GetMembers(flow)) {
                var memPath = this.path.AppendMember(relPath);
                var valueLifetime = Lifetime.None; 

                var memLocation = new InferredLocationLifetime(
                    this.Location, 
                    memPath, 
                    allowedRoots, 
                    LifetimeOrigin.LocalLocation);

                if (!memType.IsValueType(flow)) {
                    valueLifetime = new ValueLifetime(memPath, LifetimeRole.Alias, LifetimeOrigin.LocalValue);
                }

                // Add a dependency between whatever is being assigned to this variable and the
                // variable's value
                flow.LifetimeGraph.AddAssignment(
                    assignBundle[relPath].ValueLifetime,
                    valueLifetime,
                    memType);

                // The value of a variable must outlive its location
                flow.LifetimeGraph.AddStored(valueLifetime, memLocation, memType);

                // Make sure that this sub-location outlives the main location
                flow.LifetimeGraph.AddStored(memLocation, varLocation, memType);

                // Add this variable lifetimes to the current frame
                var bounds = new LifetimeBounds(valueLifetime, memLocation);
                dict[relPath] = bounds;
                flow.LocalLifetimes = flow.LocalLifetimes.SetItem(memPath, bounds);
            }

            return new LifetimeBundle(dict);
        }

        public ICSyntax GenerateCode(FlowFrame flow, ICStatementWriter writer) {
            var assign = this.assignSyntax.GenerateCode(flow, writer);
            var lifetime = this.GetLifetimes(flow)[new IdentifierPath()].LocationLifetime;
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
            writer.VariableKinds[this.path] = CVariableKind.Local;
        }

        private void GenerateRegionAllocation(ICSyntax assign, ICSyntax allocLifetime,
                                              FlowFrame flow, ICStatementWriter writer) {
            var name = writer.GetVariableName(this.path);
            var assignType = this.assignSyntax.GetReturnType(flow);
            var cReturnType = writer.ConvertType(assignType);

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