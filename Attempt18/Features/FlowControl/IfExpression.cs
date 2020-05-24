using System;
using System.Collections.Generic;
using System.Linq;
using Attempt19.Evaluation;
using Attempt19.Types;

namespace Attempt19.Features.FlowControl {
    public class IfExpression : ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public ISyntax Condition { get; set; }

        public ISyntax Affirmative { get; set; }

        public ISyntax Negative { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            this.Condition.AnalyzeFlow(types,flow);

            var affirmFlow = new IfBranchFlowCache(flow, flow.Clone());
            var negFlow = new IfBranchFlowCache(flow, flow.Clone());

            this.Affirmative.AnalyzeFlow(types, affirmFlow);
            this.Negative.AnalyzeFlow(types, negFlow);

            this.CapturedVariables = this.Affirmative.CapturedVariables
                .Concat(this.Negative.CapturedVariables)
                .Distinct()
                .ToArray();
        }

        public void DeclareNames(NameCache<NameTarget> names) {
            this.Condition.DeclareNames(names);
            this.Affirmative.DeclareNames(names);
            this.Negative.DeclareNames(names);
        }

        public void DeclareTypes(TypeChache  cache) {
            this.Condition.DeclareTypes(cache);
            this.Affirmative.DeclareTypes(cache);
            this.Negative.DeclareTypes(cache);
        }

        public IEvaluateResult Evaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            var cond = (bool)this.Condition.Evaluate(memory).Value;

            if (cond) {
                return this.Affirmative.Evaluate(memory);
            }
            else {
                return this.Negative.Evaluate(memory);
            }
        }

        public void PreEvaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            this.Condition.PreEvaluate(memory);
            this.Affirmative.PreEvaluate(memory);
            this.Negative.PreEvaluate(memory);
        }

        public void ResolveNames(NameCache<NameTarget> names) {
            this.Condition.ResolveNames(names);
            this.Affirmative.ResolveNames(names);
            this.Negative.ResolveNames(names);
        }

        public void ResolveScope(IdentifierPath containingScope) {
            this.Scope = containingScope;

            this.Condition.ResolveScope(containingScope);
            this.Affirmative.ResolveScope(containingScope);
            this.Negative.ResolveScope(containingScope);
        }

        public ISyntax ResolveTypes(TypeChache  types) {
            this.Condition = this.Condition.ResolveTypes(types);
            this.Affirmative = this.Affirmative.ResolveTypes(types);
            this.Negative = this.Negative.ResolveTypes(types);

            if (this.Condition.ReturnType.Kind != LanguageTypeKind.Bool) {
                throw new Exception();
            }

            if (this.Affirmative.ReturnType != this.Negative.ReturnType) {
                throw new Exception();
            }

            this.ReturnType = this.Affirmative.ReturnType;

            return this;
        }
    }
}