using Helix.Analysis;
using Helix.Analysis.Lifetimes;
using Helix.Analysis.Types;
using Helix.Features.Variables;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace helix.FlowAnalysis {
    public class VariableProducer {
        private readonly IdentifierPath basePath;
        private readonly HelixType baseType;
        private readonly LifetimeKind kind;

        public VariableProducer(IdentifierPath path, HelixType baseType, LifetimeKind kind) {
            this.basePath = path;
            this.baseType = baseType;
            this.kind = kind;
        }

        public void DeclareNewRoots(EvalFrame types) {
            // Go through all the variables and sub variables and set up the lifetimes
            // correctly
            foreach (var (compPath, _) in this.baseType.GetMembers(types)) {
                var memPath = this.basePath.Append(compPath);
                var lifetime = new Lifetime(memPath, 0, this.kind);

                // Add this variable's lifetime
                types.LifetimeRoots[memPath] = lifetime;
            }
        }

        public void DeclareLifetimes(FlowFrame flow) {
            var baseLifetime = new Lifetime(this.basePath, 0, this.kind);

            // Go through all the variables and sub variables and set up the lifetimes
            // correctly
            foreach (var (relPath, _) in this.baseType.GetMembers(flow)) {
                var memPath = this.basePath.Append(relPath);
                var varLifetime = new Lifetime(memPath, 0, kind);

                // Make sure we say the main lifetime outlives all of the member lifetimes
                flow.LifetimeGraph.RequireOutlives(baseLifetime, varLifetime);
                flow.LifetimeGraph.RequireOutlives(varLifetime, baseLifetime);

                // Add this variable members's lifetime
                flow.VariableLifetimes[memPath] = varLifetime;
            }
        }

        public void DeclareLifetimeContents(LifetimeBundle assignBundle, FlowFrame flow) {
            // Go through all the variables and sub variables and set up the lifetimes
            // correctly
            foreach (var (relPath, _) in this.baseType.GetMembers(flow)) {
                var path = this.basePath.Append(relPath);
                var varLifetime = new Lifetime(path, 0, this.kind);

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

        public void DeclareLifetimePaths(FlowFrame flow, ICStatementWriter writer) {
            foreach (var (relPath, _) in this.baseType.GetMembers(flow)) {
                writer.RegisterMemberPath(this.basePath, relPath);
            }
        }

        public void RegisterLifetimes(FlowFrame flow, ICStatementWriter writer, ICSyntax lifetime) {
            foreach (var (relPath, _) in this.baseType.GetMembers(flow)) {
                var memPath = this.basePath.Append(relPath);

                writer.RegisterLifetime(flow.VariableLifetimes[memPath], lifetime);
            }
        }
    }
}
