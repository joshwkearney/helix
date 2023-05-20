using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.Memory;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Parsing {
    public partial class Parser {
        private int dereferenceCounter = 0;

        public ISyntaxTree DereferenceExpression(ISyntaxTree first) {
            var op = this.Advance(TokenKind.Star);
            var loc = first.Location.Span(op.Location);

            return new DereferenceSyntax(
                loc, 
                first, 
                this.scope.Append("$deref_" + this.dereferenceCounter++));
        }
    }
}

namespace Helix.Features.Memory {
    // Dereference syntax is split into three classes: this one that does
    // some basic type checking so it's easy for the parser to spit out
    // a single class, a dereference rvalue, and a dereference lvaulue.
    // This is for clarity because dereference rvalues and lvalues have
    // very different semantics, especially when it comes to lifetimes
    public record DereferenceSyntax : ISyntaxTree {
        private readonly ISyntaxTree target;
        private readonly IdentifierPath tempPath;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public DereferenceSyntax(TokenLocation loc, ISyntaxTree target,
                                 IdentifierPath tempPath) {
            this.Location = loc;
            this.target = target;
            this.tempPath = tempPath;
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
            var result = new DereferenceSyntax(this.Location, target, this.tempPath);

            result.SetReturnType(pointerType.InnerType, types);

            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            return new DereferenceRValue(this.Location, this.target, this.tempPath).CheckTypes(types);
        }

        public ISyntaxTree ToLValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            return new DereferenceLValue(this.Location, this.target, this.tempPath).CheckTypes(types);
        }
    }

    public record DereferenceRValue : ISyntaxTree {
        private readonly ISyntaxTree target;
        private readonly IdentifierPath tempPath;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public DereferenceRValue(TokenLocation loc, ISyntaxTree target, 
            IdentifierPath tempPath) {

            this.Location = loc;
            this.target = target;
            this.tempPath = tempPath;
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

            // this.target is already type checked
            var pointerType = this.target.AssertIsPointer(types);
            this.SetReturnType(pointerType.InnerType, types);

            return this;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            if (this.IsFlowAnalyzed(flow)) {
                return;
            }

            this.target.AnalyzeFlow(flow);

            var pointerType = this.target.AssertIsPointer(flow);
            var bundleDict = new Dictionary<IdentifierPath, Lifetime>();

            // Doing this is ok because pointers don't have members
            var pointerLifetime = this.target.GetLifetimes(flow)[new IdentifierPath()];

            // Build a return bundle composed of lifetimes that outlive the pointer's lifetime
            // This loop replaces flow.DeclareValueLifetimes() because some custom logic is needed
            foreach (var (relPath, type) in pointerType.InnerType.GetMembers(flow)) {
                var memPath = this.tempPath.AppendMember(relPath);

                if (type.IsValueType(flow)) {
                    bundleDict[relPath] = Lifetime.None;
                    flow.LocalLifetimes[memPath] = new LifetimeBounds();
                }
                else {
                    // This value's lifetime actually isn't the pointer's lifetime, but some
                    // other lifetime that outlives the pointer. It's important to represent
                    // the value like this because we can't store things into it that only
                    // outlive the pointer
                    var lifetime = new ValueLifetime(memPath, LifetimeRole.Root, false);

                    // Make sure we add this as a root
                    flow.LifetimeRoots.Add(lifetime);

                    // The lifetime that is stored in the pointer must outlive the pointer itself
                    flow.LifetimeGraph.RequireOutlives(lifetime, pointerLifetime);
                    flow.LocalLifetimes[memPath] = new LifetimeBounds(Lifetime.None, lifetime);

                    bundleDict[relPath] = lifetime;
                }
            }

            this.SetLifetimes(new LifetimeBundle(bundleDict), flow);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
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
            return new CVariableLiteral(tempName);
        }
    }

    public record DereferenceLValue : ISyntaxTree {
        private readonly IdentifierPath tempPath;
        private readonly ISyntaxTree target;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public DereferenceLValue(TokenLocation loc, ISyntaxTree target, IdentifierPath tempPath) {
            this.Location = loc;
            this.target = target;
            this.tempPath = tempPath;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            this.SetReturnType(this.target.GetReturnType(types), types);
            return this;
        }

        public ISyntaxTree ToLValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            return this;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            if (this.IsFlowAnalyzed(flow)) {
                return;
            }

            // This function will be slightly weird
            // We can't return the target's lvalue directly because that would be like
            // we're setting the variable itself, when really we're setting whatever it's
            // pointing at. Therefore, we need to create a new lifetime that points in
            // both directions at the target's lvalue so that the mutation checker in
            // assignment syntax won't think we're setting a local and will properly check
            // for aliasing

            this.target.AnalyzeFlow(flow);
            var targetLifetime = this.target.GetLifetimes(flow)[new IdentifierPath()];

            var dict = new Dictionary<IdentifierPath, Lifetime>();
            var lifetime = new ValueLifetime(this.tempPath.ToVariablePath(), LifetimeRole.Alias, false);

            dict[new IdentifierPath()] = lifetime;
            this.SetLifetimes(new LifetimeBundle(dict), flow);

            flow.LifetimeGraph.RequireOutlives(targetLifetime, lifetime);
            flow.LifetimeGraph.RequireOutlives(lifetime, targetLifetime);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            var target = this.target.GenerateCode(types, writer);
            var result = new CMemberAccess() {
                Target = target,
                MemberName = "data"
            };

            return result;
        }
    }
}
