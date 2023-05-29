using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Features.Variables;

namespace Helix.Parsing {
    public partial class Parser {
        public ISyntaxTree DereferenceExpression(ISyntaxTree first) {
            var op = this.Advance(TokenKind.Star);
            var loc = first.Location.Span(op.Location);

            return new DereferenceSyntax(
                loc, 
                first);
        }
    }
}

namespace Helix.Features.Variables {
    // Dereference syntax is split into three classes: this one that does
    // some basic type checking so it's easy for the parser to spit out
    // a single class, a dereference rvalue, and a dereference lvaulue.
    // This is for clarity because dereference rvalues and lvalues have
    // very different semantics, especially when it comes to lifetimes
    public record DereferenceSyntax : ISyntaxTree {
        private static int derefCounter = 0;

        private readonly ISyntaxTree target;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public DereferenceSyntax(TokenLocation loc, ISyntaxTree target) {
            this.Location = loc;
            this.target = target;
        }

        public Option<HelixType> AsType(TypeFrame types) {
            return this.target.AsType(types)
                .Select(x => new PointerType(x, true))
                .Select(x => (HelixType)x);
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var target = this.target.CheckTypes(types).ToRValue(types);
            var pointerType = target.AssertIsPointer(types);
            var result = new DereferenceSyntax(this.Location, target);

            result.SetReturnType(pointerType.InnerType, types);
            result.SetCapturedVariables(target, types);
            result.SetPredicate(target, types);

            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            var path = types.Scope.Append("$deref" + derefCounter++);
            return new DereferenceRValue(this.Location, this.target, path).CheckTypes(types);
        }

        public ISyntaxTree ToLValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            var path = types.Scope.Append("$deref" + derefCounter++);
            return new DereferenceLValue(this.Location, this.target, path).CheckTypes(types);
        }
    }

    public record DereferenceRValue : ISyntaxTree {
        private readonly ISyntaxTree target;
        private readonly IdentifierPath tempPath;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public DereferenceRValue(
            TokenLocation loc, 
            ISyntaxTree target, 
            IdentifierPath tempPath) {

            this.Location = loc;
            this.target = target;
            this.tempPath = tempPath;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            // this.target is already type checked
            var pointerType = this.target.AssertIsPointer(types);

            this.SetReturnType(pointerType.InnerType, types);
            this.SetCapturedVariables(this.target, types);
            this.SetPredicate(this.target, types);
            this.SetLifetimes(AnalyzeFlow(this.tempPath, this.target, types), types);

            return this;
        }

        private static LifetimeBounds AnalyzeFlow(IdentifierPath tempPath, ISyntaxTree target, TypeFrame flow) {
            var pointerType = target.AssertIsPointer(flow);
            var pointerLifetime = target.GetLifetimes(flow);

            if (pointerType.InnerType.IsValueType(flow)) {
                flow.LocalLifetimes = flow.LocalLifetimes.SetItem(tempPath, new LifetimeBounds());
                return new LifetimeBounds();
            }

            // If we are dereferencing a pointer and the following three conditions hold,
            // we don't have to make up a new lifetime: 1) We're dereferencing a local variable
            // 2) That local variable could not have been mutated by an alias since the last 
            // time it was set 3) That local variable is storing the location of another variable
            if (pointerLifetime.LocationLifetime != Lifetime.None) {
                var valueLifetime = flow.LocalLifetimes[pointerLifetime.LocationLifetime.Path].ValueLifetime;

                var equivalents = flow
                    .DataFlowGraph
                    .GetEquivalentLifetimes(valueLifetime)
                    .Where(x => x.Origin == LifetimeOrigin.LocalLocation);

                // If all three are true, we can return the location of the that variable
                // whose location is currently stored in the variable we're dereferencing.
                // Think of this as optimizing dereferencing an addressof operator.
                if (equivalents.Any()) {
                    return new LifetimeBounds(equivalents.First());
                }
            }

            // This value's lifetime actually isn't the pointer's lifetime, but some
            // other lifetime that outlives the pointer. It's important to represent
            // the value like this because we can't store things into it that only
            // outlive the pointer
            var derefValueLifetime = new ValueLifetime(tempPath, LifetimeRole.Root, LifetimeOrigin.TempValue);

            // Make sure we add this as a root
            flow.LifetimeRoots = flow.LifetimeRoots.Add(derefValueLifetime);

            // The lifetime that is stored in the pointer must outlive the pointer itself
            flow.DataFlowGraph.AddStored(derefValueLifetime, pointerLifetime.ValueLifetime);

            return new LifetimeBounds(derefValueLifetime);
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var target = this.target.GenerateCode(types, writer);
            var pointerType = this.target.AssertIsPointer(types);
            var tempName = writer.GetVariableName(this.tempPath);
            var tempType = writer.ConvertType(pointerType.InnerType);

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: Pointer dereference");
            writer.WriteStatement(new CVariableDeclaration() {
                Name = tempName,
                Type = tempType,
                Assignment = new CPointerDereference() {
                    Target = new CMemberAccess() {
                        Target = target,
                        MemberName = "data",
                        IsPointerAccess = false
                    }
                }
            });

            writer.WriteEmptyLine();
            writer.VariableKinds[this.tempPath] = CVariableKind.Local;

            return new CVariableLiteral(tempName);
        }
    }

    public record DereferenceLValue : ISyntaxTree {
        private readonly ISyntaxTree target;
        private readonly IdentifierPath tempPath;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public DereferenceLValue(TokenLocation loc, ISyntaxTree target, IdentifierPath tempPath) {
            this.Location = loc;
            this.target = target;
            this.tempPath = tempPath;
        }

        public ISyntaxTree ToLValue(TypeFrame types) => this;

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            this.SetReturnType(this.target.GetReturnType(types), types);
            this.SetCapturedVariables(this.target, types);
            this.SetPredicate(this.target, types);
            this.SetLifetimes(AnalyzeFlow(this.tempPath, this.target, types), types);

            return this;
        }

        private static LifetimeBounds AnalyzeFlow(IdentifierPath tempPath, ISyntaxTree target, TypeFrame flow) {
            var targetBounds = target.GetLifetimes(flow);

            // If we are dereferencing a pointer and the following three conditions hold,
            // we don't have to make up a new lifetime: 1) We're dereferencing a local variable
            // 2) That local variable is storing the location of another variable
            if (AnalyzeLocalDeref(targetBounds, flow, out var bounds)) {
                return bounds;
            }

            var derefValueLifetime = new ValueLifetime(
                    tempPath,
                    LifetimeRole.Alias,
                    LifetimeOrigin.TempValue);

            var precursors = flow.DataFlowGraph
                .GetPrecursorLifetimes(targetBounds.ValueLifetime)
                .ToArray();

            // We could potentially be storing into anything upstream of our target
            // with pointer aliasing, so assume that is the case and add the correct
            // dependencies
            foreach (var root in precursors) {
                flow.DataFlowGraph.AddStored(derefValueLifetime, root);
            }

            // The lifetime that is stored in the pointer must outlive the pointer itself
            flow.DataFlowGraph.AddStored(derefValueLifetime, targetBounds.ValueLifetime);

            return new LifetimeBounds(derefValueLifetime, targetBounds.ValueLifetime);
        }

        private static bool AnalyzeLocalDeref(LifetimeBounds targetBounds, TypeFrame flow, out LifetimeBounds bounds) {
            if (targetBounds.LocationLifetime == Lifetime.None) {
                bounds = default;
                return false;
            }

            var valueLifetime = flow.LocalLifetimes[targetBounds.LocationLifetime.Path].ValueLifetime;
            var dict = new Dictionary<IdentifierPath, LifetimeBounds>();

            var equivalents = flow
                .DataFlowGraph
                .GetEquivalentLifetimes(valueLifetime)
                .Where(x => x.Origin == LifetimeOrigin.LocalLocation); ;

            // If all three are true, we can return the location of the that variable
            // whose location is currently stored in the variable we're dereferencing.
            // Think of this as optimizing dereferencing an addressof operator.
            if (!equivalents.Any()) {
                bounds = default;
                return false;
            }

            var loc = equivalents.First();
            var value = flow.LocalLifetimes[loc.Path].ValueLifetime;

            bounds = new LifetimeBounds(value, loc);
            return true;
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var target = this.target.GenerateCode(types, writer);
            var result = new CMemberAccess() {
                Target = target,
                MemberName = "data"
            };

            return result;
        }
    }
}
