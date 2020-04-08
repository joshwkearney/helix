using System;
using System.Collections.Generic;
using System.Linq;
using Attempt18.Types;

namespace Attempt18.Features.Containers.Arrays {
    public class ArrayLiteralSyntax : ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public ISyntax[] Values { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            foreach (var val in this.Values) {
                val.AnalyzeFlow(types, flow);
            }

            this.CapturedVariables = this.Values
                .SelectMany(x => x.CapturedVariables)
                .Distinct()
                .ToArray();
        }

        public void DeclareNames(NameCache<NameTarget> names) {
            foreach (var val in this.Values) {
                val.DeclareNames(names);
            }
        }

        public void DeclareTypes(TypeChache  cache) {
            foreach (var val in this.Values) {
                val.DeclareTypes(cache);
            }
        }

        public object Evaluate(Dictionary<IdentifierPath, object> memory) {
            return this.Values
                .Select(x => x.Evaluate(memory))
                .Select(x => (object)x)
                .ToArray();
        }

        public void PreEvaluate(Dictionary<IdentifierPath, object> memory) {
            foreach (var value in this.Values) {
                value.PreEvaluate(memory);
            }
        }

        public void ResolveNames(NameCache<NameTarget> names) {
            foreach (var val in this.Values) {
                val.ResolveNames(names);
            }
        }

        public void ResolveScope(IdentifierPath containingScope) {
            this.Scope = containingScope;

            foreach (var val in this.Values) {
                val.ResolveScope(containingScope);
            }
        }

        public ISyntax ResolveTypes(TypeChache  types) {
            // Make sure there is at least one value
            if (!this.Values.Any()) {
                throw new Exception();
            }

            // Type check the values
            this.Values = this.Values.Select(x => x.ResolveTypes(types)).ToArray();

            // Make sure all values match
            var type = this.Values.First().ReturnType;
            foreach (var val in this.Values) {
                if (val.ReturnType != type) {
                    throw new Exception();
                }
            }

            this.ReturnType = new ArrayType(type);

            return this;
        }
    }
}
