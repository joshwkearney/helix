using System;
using System.Collections.Generic;
using Attempt18.Types;

namespace Attempt18.Features.Variables {
    public class VariableLiteralAccess : ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public string VariableName { get; set; }

        public IdentifierPath VariablePath { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            // You can't access a moved variable
            if (flow.IsVariableMoved(this.VariablePath)) {
                throw new Exception("Cannot access a moved variable");
            }

            // Capture the variable
            this.CapturedVariables = new[] { this.VariablePath };
        }

        public void DeclareNames(NameCache<NameTarget> names) { }

        public void DeclareTypes(TypeChache  cache) { }

        public object Evaluate(Dictionary<IdentifierPath, object> memory) {
            return this.VariablePath;
        }

        public void PreEvaluate(Dictionary<IdentifierPath, object> memory) { }

        public void ResolveNames(NameCache<NameTarget> names) {
            // Make sure this name exists
            if (!names.FindName(this.Scope, this.VariableName, out var varPath, out var target)) {
                throw new Exception();
            }

            // Make sure this name is a variable
            if (target != NameTarget.Variable) {
                throw new Exception();
            }

            this.VariablePath = varPath;
        }

        public void ResolveScope(IdentifierPath containingScope) {
            this.Scope = containingScope;
        }

        public ISyntax ResolveTypes(TypeChache  types) {
            this.ReturnType = new VariableType(types.Variables[this.VariablePath].Type);

            return this;
        }
    }
}
