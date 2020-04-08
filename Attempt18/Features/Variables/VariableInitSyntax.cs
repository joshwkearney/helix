using System;
using System.Collections.Generic;
using Attempt18.Types;

namespace Attempt18.Features.Variables {
    public class VariableInitSyntax : ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath VariablePath { get; set; }

        public string VariableName { get; set; }

        public ISyntax Value { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            this.Value.AnalyzeFlow(types, flow);

            // Capture the variables that this value depends on for the entire scope
            foreach (var cap in this.Value.CapturedVariables) {
                flow.RegisterDependency(this.VariablePath, cap);
            }

            this.CapturedVariables = new IdentifierPath[0];
        }

        public void DeclareNames(NameCache<NameTarget> names) {
            this.Value.DeclareNames(names);
        }

        public void DeclareTypes(TypeChache  cache) {
            this.Value.DeclareTypes(cache);
        }

        public object Evaluate(Dictionary<IdentifierPath, object> memory) {
            memory[this.VariablePath] = this.Value.Evaluate(memory);

            return 0;
        }

        public void PreEvaluate(Dictionary<IdentifierPath, object> memory) {
            this.Value.PreEvaluate(memory);
        }

        public void ResolveNames(NameCache<NameTarget> names) {
            this.Value.ResolveNames(names);

            names.AddLocalName(this.VariablePath, NameTarget.Variable);

            // If there is no containing scope, there can't be a name conflict
            if (this.Scope.Segments.IsEmpty) {
                return;
            }

            // Otherwise, see if somebody higher up took the name
            if (names.FindName(this.Scope.Pop(), this.VariableName, out _, out _)) {
                throw new Exception();
            }
        }

        public void ResolveScope(IdentifierPath containingScope) {
            this.Scope = containingScope;
            this.VariablePath = this.Scope.Append(this.VariableName);

            this.Value.ResolveScope(containingScope);
        }

        public ISyntax ResolveTypes(TypeChache  types) {
            this.Value = this.Value.ResolveTypes(types);

            var info = new VariableInfo() {
                DefinitionKind = VariableDefinitionKind.Local,
                IsFunctionParameter = false,
                Type = this.Value.ReturnType
            };

            types.Variables.Add(this.VariablePath, info);

            this.ReturnType = VoidType.Instance;

            return this;
        }
    }
}
