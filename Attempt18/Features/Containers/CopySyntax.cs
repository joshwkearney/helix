using System;
using System.Collections.Generic;
using System.Linq;
using Attempt18.Types;

namespace Attempt18.Features.Containers {
    public class CopySyntax : ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public ISyntax Target { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            this.Target.AnalyzeFlow(types, flow);

            this.CapturedVariables = this.Target.CapturedVariables.ToArray();
        }

        public void DeclareNames(NameCache<NameTarget> names) {
            throw new InvalidOperationException();
        }

        public void DeclareTypes(TypeChache cache) {
            throw new InvalidOperationException();
        }

        public object Evaluate(Dictionary<IdentifierPath, object> memory) {
            var target = this.Target.Evaluate(memory);

            switch (this.Target.ReturnType.Kind) {
                case LanguageTypeKind.Bool:
                case LanguageTypeKind.Int:
                case LanguageTypeKind.Void:
                case LanguageTypeKind.Array:
                case LanguageTypeKind.Variable:
                case LanguageTypeKind.Function:
                    return target;

                case LanguageTypeKind.Struct:
                    var structValue = (Dictionary<string, object>)target;
                    return structValue.ToDictionary(x => x.Key, x => x.Value);

                default:
                    throw new Exception();
            }
        }

        public void PreEvaluate(Dictionary<IdentifierPath, object> memory) {
            this.Target.PreEvaluate(memory);
        }

        public void ResolveNames(NameCache<NameTarget> names) {
            throw new InvalidOperationException();
        }

        public void ResolveScope(IdentifierPath containingScope) {
            throw new InvalidOperationException();
        }

        public ISyntax ResolveTypes(TypeChache types) {
            throw new InvalidOperationException();
        }
    }
}
