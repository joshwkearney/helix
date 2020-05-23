using System;
using System.Collections.Generic;
using System.Linq;
using Attempt18.Evaluation;
using Attempt18.Types;

namespace Attempt18.Features.Variables {
    public class MoveSyntax : ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public string VariableName { get; set; }

        public IdentifierPath VariablePath { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            // Make sure the variable is not already moved
            if (flow.IsVariableMoved(this.VariablePath)) {
                throw new Exception("Cannot move already moved variable");
            }

            // Make sure the variable is not captured by anything
            if (flow.GetDependentVariables(this.VariablePath).Except(new[] { this.VariablePath }).Any()) {
                throw new Exception("Cannot move captured variable");
            }

            // Makes sure we're not moving inside of a while loop
            if (this.Scope.Segments.Any(x => x.StartsWith("_while"))) {
                throw new Exception("Cannot move inside of while loop");
            }

            // Set this variable moved
            flow.SetVariableMoved(this.VariablePath, true);

            this.CapturedVariables = flow
                .GetAncestorVariables(this.VariablePath)
                .Except(new[] { this.VariablePath })
                .ToArray();
        }

        public void DeclareNames(NameCache<NameTarget> names) { }

        public void DeclareTypes(TypeChache  cache) { }

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
            if (target != NameTarget.Variable) {
                throw new Exception();
            }

            this.VariablePath = varPath;
        }

        public void ResolveScope(IdentifierPath containingScope) {
            this.Scope = containingScope;
        }

        public ISyntax ResolveTypes(TypeChache  types) {
            this.ReturnType = types.Variables[this.VariablePath].Type;

            return this;
        }
    }
}