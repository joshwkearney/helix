using System;
using System.Collections.Generic;
using Attempt19.Evaluation;
using Attempt19.Features.Containers;
using Attempt19.Types;

namespace Attempt19.Features.Variables {
    public class VariableAccessSyntax : ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public string VariableName { get; set; }

        public IdentifierPath VariablePath { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            // You can't access a moved variable
            if (flow.IsVariableMoved(this.VariablePath)) {
                throw new Exception("Cannot access a moved variable");
            }

            // If this variable type is conditionally copiable, be sure to capture it
            if (this.ReturnType.GetCopiability(types) == Copiability.Unconditional) {
                this.CapturedVariables = new IdentifierPath[0];
            }
            else {
                this.CapturedVariables = flow.GetAncestorVariables(this.VariablePath);
            }
        }

        public void DeclareNames(NameCache<NameTarget> names) { }

        public void DeclareTypes(TypeChache cache) { }

        public IEvaluateResult Evaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            return memory[this.VariablePath];
        }

        public void PreEvaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) { }

        public void ResolveNames(NameCache<NameTarget> names) {
            // Make sure this name exists
            if (!names.FindName(this.Scope, this.VariableName, out var varPath, out var target)) {
                throw new Exception();
            }

            // Make sure this name is a variable
            if (target != NameTarget.Variable && target != NameTarget.Function) {
                throw new Exception();
            }

            this.VariablePath = varPath;
        }

        public void ResolveScope(IdentifierPath containingScope) {
            this.Scope = containingScope;
        }

        public ISyntax ResolveTypes(TypeChache types) {
            if (types.Variables.TryGetValue(this.VariablePath, out var info)) {
                this.ReturnType = info.Type;
            }
            else if (types.Functions.TryGetValue(this.VariablePath, out var _)) {
                this.ReturnType = new FunctionType(this.VariablePath);
            }
            else {
                throw new Exception();
            }

            // You can't access a non-copiable type
            if (this.ReturnType.GetCopiability(types) == Copiability.None) {
                throw new Exception();
            }

            return new CopySyntax() {
                ReturnType = this.ReturnType,
                Scope = this.Scope,
                Target = this
            };
        }
    }
}
