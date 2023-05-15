using helix.FlowAnalysis;
using Helix.Analysis;
using Helix.Analysis.Lifetimes;
using Helix.Analysis.Types;
using Helix.Features.Variables;
using Helix.Generation;
using Helix.Generation.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace helix.Syntax.Decorators {
    public class LifetimeProducer : ISyntaxDecorator {
        public IdentifierPath LifetimePath { get; }

        public HelixType LifetimeType { get; }

        public LifetimeKind LifetimeKind { get; }

        public LifetimeProducer(IdentifierPath path, HelixType baseType, LifetimeKind kind) {
            LifetimePath = path;
            LifetimeType = baseType;
            LifetimeKind = kind;
        }

        public virtual void PostCheckTypes(ISyntaxTree syntax, EvalFrame types) {
            // Go through all the variables and sub variables and set up the lifetimes
            // correctly
            foreach (var (compPath, _) in LifetimeType.GetMembers(types)) {
                var memPath = LifetimePath.Append(compPath);
                var lifetime = new Lifetime(memPath, 0, LifetimeKind);

                // Add this variable's lifetime
                types.LifetimeRoots[memPath] = lifetime;
            }
        }

        public virtual void PostAnalyzeFlow(ISyntaxTree syntax, FlowFrame flow) {
            var baseLifetime = new Lifetime(LifetimePath, 0, LifetimeKind);

            // Go through all the variables and sub variables and set up the lifetimes
            // correctly
            foreach (var (relPath, _) in LifetimeType.GetMembers(flow)) {
                var memPath = LifetimePath.Append(relPath);
                var varLifetime = new Lifetime(memPath, 0, LifetimeKind);

                // Make sure we say the main lifetime outlives all of the member lifetimes
                flow.LifetimeGraph.RequireOutlives(baseLifetime, varLifetime);
                flow.LifetimeGraph.RequireOutlives(varLifetime, baseLifetime);

                // Add this variable members's lifetime
                flow.VariableLifetimes[memPath] = varLifetime;
            }
        }

        public virtual void PostGenerateCode(ISyntaxTree syntax, ICSyntax result, FlowFrame flow, ICStatementWriter writer) {
            // TODO: Make this better
            // Register our member paths so the code generator knows how to get to them            
            foreach (var (relPath, _) in LifetimeType.GetMembers(flow)) {
                writer.RegisterMemberPath(LifetimePath, relPath);
            }

            // Figure out the c vlaue for our new lifetime based on the lifetimes it
            // is supposed to outlive
            var roots = flow
                .LifetimeGraph
                .GetOutlivedLifetimes(flow.VariableLifetimes[this.LifetimePath])
                .Where(x => x.Kind != LifetimeKind.Inferencee);

            roots = flow.ReduceRootSet(roots);

            var cLifetime = writer.CalculateSmallestLifetime(syntax.Location, roots);

            foreach (var (relPath, type) in LifetimeType.GetMembers(flow)) {
                var memPath = this.LifetimePath.Append(relPath);

                writer.RegisterLifetime(flow.VariableLifetimes[memPath], cLifetime);
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

        public override void PostAnalyzeFlow(ISyntaxTree syntax, FlowFrame flow) {
            base.PostAnalyzeFlow(syntax, flow);

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