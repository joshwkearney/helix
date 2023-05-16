using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;

namespace Helix.Syntax.Decorators {
    public class LifetimeProducer : ISyntaxDecorator {
        public IdentifierPath OriginPath { get; }

        public HelixType OriginType { get; }

        public LifetimeKind LifetimeKind { get; }

        public LifetimeProducer(IdentifierPath path, HelixType baseType, LifetimeKind kind) {
            this.OriginPath = path;
            this.OriginType = baseType;
            this.LifetimeKind = kind;
        }

        public virtual void PostCheckTypes(ISyntaxTree syntax, TypeFrame types) {
            // Go through all the variables and sub variables and set up the lifetimes
            // correctly
            foreach (var (relPath, _) in this.OriginType.GetMembers(types)) {
                var varPath = new VariablePath(this.OriginPath, relPath);
                var lifetime = new Lifetime(varPath, 0, this.LifetimeKind);

                // Add this variable's lifetime
                types.LifetimeRoots[varPath] = lifetime;
            }
        }

        public virtual void PreAnalyzeFlow(ISyntaxTree syntax, FlowFrame flow) {
            var baseLifetime = new Lifetime(
                new VariablePath(this.OriginPath), 
                0, 
                this.LifetimeKind);

            // Go through all the variables and sub variables and set up the lifetimes
            // correctly
            foreach (var (relPath, _) in this.OriginType.GetMembers(flow)) {
                var memPath = new VariablePath(this.OriginPath, relPath);
                var varLifetime = new Lifetime(memPath, 0, this.LifetimeKind);

                // Make sure we say the main lifetime outlives all of the member lifetimes
                flow.LifetimeGraph.RequireOutlives(baseLifetime, varLifetime);

                // Add this variable members's lifetime
                flow.VariableLocationLifetimes[memPath] = varLifetime;
            }
        }

        public virtual void PostGenerateCode(ISyntaxTree syntax, ICSyntax result, FlowFrame flow, ICStatementWriter writer) {
            var basePath = new VariablePath(this.OriginPath);

            // Figure out the c vlaue for our new lifetime based on the lifetimes it
            // is supposed to outlive
            var roots = flow
                .LifetimeGraph
                .GetOutlivedLifetimes(flow.VariableLocationLifetimes[basePath])
                .Where(x => x.Kind != LifetimeKind.Inferencee);

            roots = flow.ReduceRootSet(roots);

            if (this.LifetimeKind == LifetimeKind.Inferencee) {
                // This lifetime is inferred, which means it is being calculated from previous
                // lifetimes
                var cLifetime = writer.CalculateSmallestLifetime(syntax.Location, roots);

                foreach (var (relPath, type) in this.OriginType.GetMembers(flow)) {
                    var memPath = new VariablePath(this.OriginPath, relPath);

                    writer.RegisterLifetime(flow.VariableLocationLifetimes[memPath], cLifetime);
                }
            }
            else {
                // This is a new lifetime declaration
                foreach (var (relPath, type) in this.OriginType.GetMembers(flow)) {
                    var memPath = new VariablePath(this.OriginPath, relPath);
                    var memAccess = result;

                    // Only register lifetimes that exist
                    if (type.IsValueType(flow)) {
                        continue;
                    }

                    foreach (var segment in relPath.Segments) {
                        memAccess = new CMemberAccess() {
                            IsPointerAccess = false,
                            MemberName = segment,
                            Target = memAccess
                        };
                    }

                    memAccess = new CMemberAccess() {
                        IsPointerAccess = false,
                        MemberName = "region",
                        Target = memAccess
                    };

                    writer.RegisterLifetime(flow.VariableLocationLifetimes[memPath], memAccess);
                }
            }
        }
    }

    public class AssignedLifetimeProducer : LifetimeProducer {
        public ISyntaxTree AssignSyntax { get; }

        public AssignedLifetimeProducer(IdentifierPath lifetimePath, HelixType baseType,
                                        LifetimeKind kind, ISyntaxTree assign)
            : base(lifetimePath, baseType, kind) {

            AssignSyntax = assign;
        }

        public void PostAnalyzeFlow(ISyntaxTree syntax, FlowFrame flow) {
            base.PreAnalyzeFlow(syntax, flow);

            var assignBundle = AssignSyntax.GetLifetimes(flow);

            // Go through all the variables and sub variables and set up the lifetimes
            // correctly
            foreach (var (relPath, _) in OriginType.GetMembers(flow)) {
                var path = new VariablePath(this.OriginPath, relPath);
                var varLifetime = new Lifetime(path, 0, LifetimeKind);

                // Add a dependency between this version of the variable lifetime
                // and the assigned expression. Whenever an alias might occur the
                // version will be incremented, so this will not be unsafe with
                // mutable variables
                flow.LifetimeGraph.RequireOutlives(
                    assignBundle[relPath],
                    varLifetime);

                // Add this variable members's lifetime
                flow.VariableValueLifetimes[path] = assignBundle[relPath];
            }
        }
    }
}