using System;
using System.Collections.Generic;
using Attempt19.Evaluation;
using Attempt19.Types;

namespace Attempt19.Features.Containers.Arrays {
    public class IndexAccess : ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public ISyntax Target { get; set; }

        public ISyntax Index { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            this.Target.AnalyzeFlow(types, flow);
            this.Index.AnalyzeFlow(types, flow);

            if (this.ReturnType.GetCopiability(types) == Copiability.Unconditional) {
                this.CapturedVariables = new IdentifierPath[0];
            }
            else {
                this.CapturedVariables = this.Target.CapturedVariables;
            }
        }

        public void DeclareNames(NameCache<NameTarget> names) {
            this.Target.DeclareNames(names);
            this.Index.DeclareNames(names);
        }

        public void DeclareTypes(TypeChache cache) {
            this.Target.DeclareTypes(cache);
            this.Index.DeclareTypes(cache);
        }

        public IEvaluateResult Evaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            var target = (IEvaluateResult[])this.Target.Evaluate(memory).Value;
            var index = (long)this.Index.Evaluate(memory).Value;

            return target[index];
        }

        public void PreEvaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            this.Target.PreEvaluate(memory);
            this.Index.PreEvaluate(memory);
        }

        public void ResolveNames(NameCache<NameTarget> names) {
            this.Target.ResolveNames(names);
            this.Index.ResolveNames(names);
        }

        public void ResolveScope(IdentifierPath containingScope) {
            this.Scope = containingScope;

            this.Target.ResolveScope(containingScope);
            this.Index.ResolveScope(containingScope);
        }

        public ISyntax ResolveTypes(TypeChache types) {
            this.Target = this.Target.ResolveTypes(types);
            this.Index = this.Index.ResolveTypes(types);

            // Make sure the target is an array type
            if (!(this.Target.ReturnType is ArrayType arrType)) {
                throw new Exception();
            }

            // Make sure the index is an integer
            if (this.Index.ReturnType != IntType.Instance) {
                throw new Exception();
            }

            this.ReturnType = arrType.ElementType;

            // Can't index a non-copiable type
            if (this.ReturnType.GetCopiability(types) == Copiability.None) {
                throw new Exception();
            }

            return this;
        }
    }
}