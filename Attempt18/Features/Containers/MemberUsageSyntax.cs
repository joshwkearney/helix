using System;
using System.Collections.Generic;
using Attempt19.Evaluation;
using Attempt19.Types;

namespace Attempt19.Features.Containers {
    public class MemberUsageSyntax : ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }

        public IdentifierPath[] CapturedVariables {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }

        public ISyntax Target { get; set; }

        public IMemberUsageSegment[] Segments { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            throw new InvalidOperationException();
        }

        public void DeclareNames(NameCache<NameTarget> names) {
            this.Target.DeclareNames(names);
        }

        public void DeclareTypes(TypeChache  cache) {
            throw new InvalidOperationException();
        }

        public IEvaluateResult Evaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            throw new InvalidOperationException();
        }

        public void PreEvaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            throw new NotImplementedException();
        }

        public void ResolveNames(NameCache<NameTarget> names) {
            this.Target.ResolveNames(names);
        }

        public void ResolveScope(IdentifierPath containingScope) {
            this.Scope = containingScope;
            this.Target.ResolveScope(containingScope);
        }

        public ISyntax ResolveTypes(TypeChache types) {
            this.Target = this.Target.ResolveTypes(types);

            IMemberAccessTarget target = new ValueMemberAccessTarget(this.Target, types);

            foreach (var segment in this.Segments) {
                target = segment.Apply(target);
            }

            return target.ToSyntax();
        }
    }
}