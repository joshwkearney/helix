using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Attempt18.Evaluation;
using Attempt18.Types;

namespace Attempt18.Features.Variables {
    public class VariableStoreSyntax : ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public ISyntax Target { get; set; }

        public ISyntax Value { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            this.Target.AnalyzeFlow(types, flow);
            this.Value.AnalyzeFlow(types, flow);

            var dependents = this.Target.CapturedVariables
                .SelectMany(flow.GetDependentVariables)
                .ToImmutableHashSet()
                .Except(this.Target.CapturedVariables);

            // The variables being stored into cannot be captured by anything else because that
            // could leave memory in an invalid state
            if (dependents.Any()) {
                throw new Exception("The target of a store cannot be captured");
            }

            var capped = this.Value
                .CapturedVariables
                .SelectMany(x => flow.GetAncestorVariables(x))
                .Distinct()
                .ToArray();

            var targeted = this.Target
                .CapturedVariables
                .SelectMany(x => flow.GetAncestorVariables(x))
                .Distinct()
                .ToArray();

            // The variables captured in the value assignment must outlive the target
            foreach (var cap in capped) {
                foreach (var target in targeted) {
                    var capScope = cap.Pop();
                    var targetScope = target.Pop();

                    if (capScope != targetScope && capScope.StartsWith(targetScope)) {
                        throw new Exception();
                    }
                }
            }

            // The variable being stored into cannot be captured by the value being
            // assigned to it
            //foreach (var target in this.Target.CapturedVariables) {
            //    if (flow.GetDependentVariables.IsConnected(target, this.Value.CapturedVariables)) {
            //        throw new Exception("Variable store is self-capturing");
            //    }
            // }

            // Add the new captured variables to the graph
            foreach (var cap in capped) {
                foreach (var target in targeted) {
                    flow.RegisterDependency(target, cap);
                }
            }

            this.CapturedVariables = new IdentifierPath[0];
        }

        public void DeclareNames(NameCache<NameTarget> names) {
            this.Target.DeclareNames(names);
            this.Value.DeclareNames(names);
        }

        public void DeclareTypes(TypeChache cache) {
            this.Target.DeclareTypes(cache);
            this.Value.DeclareTypes(cache);
        }

        public IEvaluateResult Evaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            var target = (IdentifierPath)this.Target.Evaluate(memory).Value;

            memory[target] = this.Value.Evaluate(memory);

            return new AtomicEvaluateResult(0);
        }

        public void PreEvaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            this.Target.PreEvaluate(memory);
            this.Value.PreEvaluate(memory);
        }

        public void ResolveNames(NameCache<NameTarget> names) {
            this.Target.ResolveNames(names);
            this.Value.ResolveNames(names);
        }

        public void ResolveScope(IdentifierPath containingScope) {
            this.Scope = containingScope;

            this.Target.ResolveScope(containingScope);
            this.Value.ResolveScope(containingScope);
        }

        public ISyntax ResolveTypes(TypeChache types) {
            this.Target = this.Target.ResolveTypes(types);
            this.Value = this.Value.ResolveTypes(types);

            // Make sure that the target's type is a variable type
            if (!(this.Target.ReturnType is VariableType varType)) {
                throw new Exception();
            }

            // Make sure that the value's type matches the target's type
            if (varType.InnerType != this.Value.ReturnType) {
                throw new Exception();
            }

            this.ReturnType = VoidType.Instance;

            return this;
        }
    }
}
