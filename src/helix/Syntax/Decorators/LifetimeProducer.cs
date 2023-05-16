using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;

namespace Helix.Syntax.Decorators {
    public class LifetimeProducer : ISyntaxDecorator {
        public IdentifierPath LifetimePath { get; }

        public HelixType LifetimeType { get; }

        public LifetimeKind LifetimeKind { get; }

        public LifetimeProducer(IdentifierPath path, HelixType baseType, LifetimeKind kind) {
            this.LifetimePath = path;
            this.LifetimeType = baseType;
            this.LifetimeKind = kind;
        }

        public virtual void PostCheckTypes(ISyntaxTree syntax, TypeFrame types) {
            // Go through all the variables and sub variables and set up the lifetimes
            // correctly
            foreach (var (relPath, _) in this.LifetimeType.GetMembers(types)) {
                var memPath = LifetimePath.Append(relPath);
                var lifetime = new Lifetime(memPath, 0, this.LifetimeKind);

                // Add this variable's lifetime
                types.LifetimeRoots[memPath] = lifetime;
            }
        }

        public virtual void PreAnalyzeFlow(ISyntaxTree syntax, FlowFrame flow) {
            var baseLifetime = new Lifetime(this.LifetimePath, 0, this.LifetimeKind);

            // Go through all the variables and sub variables and set up the lifetimes
            // correctly
            foreach (var (relPath, _) in this.LifetimeType.GetMembers(flow)) {
                var memPath = this.LifetimePath.Append(relPath);
                var varLifetime = new Lifetime(memPath, 0, this.LifetimeKind);

                // Make sure we say the main lifetime outlives all of the member lifetimes
                flow.LifetimeGraph.RequireOutlives(baseLifetime, varLifetime);

                // Add this variable members's lifetime
                flow.VariableLifetimes[memPath] = varLifetime;
            }
        }

        public virtual void PostGenerateCode(ISyntaxTree syntax, ICSyntax result, FlowFrame flow, ICStatementWriter writer) {
            // TODO: Make this better
            // Register our member paths so the code generator knows how to get to them            
            foreach (var (relPath, _) in LifetimeType.GetMembers(flow)) {
                writer.RegisterMemberPath(this.LifetimePath, relPath);
            }

            // Figure out the c vlaue for our new lifetime based on the lifetimes it
            // is supposed to outlive
            var roots = flow
                .LifetimeGraph
                .GetOutlivedLifetimes(flow.VariableLifetimes[this.LifetimePath])
                .Where(x => x.Kind != LifetimeKind.Inferencee);

            roots = flow.ReduceRootSet(roots);

            if (this.LifetimeKind == LifetimeKind.Inferencee) {
                // This lifetime is inferred, which means it is being calculated from previous
                // lifetimes
                var cLifetime = writer.CalculateSmallestLifetime(syntax.Location, roots);

                foreach (var (relPath, type) in this.LifetimeType.GetMembers(flow)) {
                    var memPath = this.LifetimePath.Append(relPath);

                    writer.RegisterLifetime(flow.VariableLifetimes[memPath], cLifetime);
                }
            }
            else {
                // This is a new lifetime declaration
                foreach (var (relPath, type) in this.LifetimeType.GetMembers(flow)) {
                    var memPath = this.LifetimePath.Append(relPath);
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

                    writer.RegisterLifetime(flow.VariableLifetimes[memPath], memAccess);
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
            foreach (var (relPath, _) in LifetimeType.GetMembers(flow)) {
                var path = LifetimePath.Append(relPath);
                var varLifetime = new Lifetime(path, 0, LifetimeKind);

                // Add a dependency between this version of the variable lifetime
                // and the assigned expression. Whenever an alias might occur the
                // version will be incremented, so this will not be unsafe with
                // mutable variables
                flow.LifetimeGraph.RequireOutlives(
                    assignBundle.Components[relPath],
                    varLifetime);

                // Add this variable members's lifetime
                flow.VariableValueLifetimes[path] = assignBundle.Components[relPath];
            }
        }
    }
}