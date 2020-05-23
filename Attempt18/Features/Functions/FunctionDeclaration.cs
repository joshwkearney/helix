using System;
using System.Collections.Generic;
using System.Linq;
using Attempt18.Evaluation;
using Attempt18.Types;

namespace Attempt18.Features.Functions {
    public class FunctionDeclaration : ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public FunctionSignature Signature { get; set; }

        public ISyntax FunctionBody { get; set; }

        public IdentifierPath FunctionPath { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            // Add unknown dependencies
            foreach (var mem in this.Signature.Parameters) {
                var memPath = this.FunctionPath.Append(mem.Name);

                flow.RegisterDependency(memPath, IdentifierPath.UnknownPath);
            }

            // Analyze body
            this.FunctionBody.AnalyzeFlow(types, flow);

            // Make sure that the return value doesn't capture any variable inside the scope
            foreach (var cap in this.FunctionBody.CapturedVariables) {
                if (cap.StartsWith(this.FunctionPath)) {
                    throw new Exception();
                }
            }

            this.CapturedVariables = new IdentifierPath[0];
        }

        public void DeclareNames(NameCache<NameTarget> names) {
            var path = this.Scope.Append(this.Signature.Name);

            names.AddGlobalName(path, NameTarget.Function);
        }

        public void DeclareTypes(TypeChache cache) {
            cache.Functions.Add(this.FunctionPath, this.Signature);

            foreach (var par in this.Signature.Parameters) {
                var path = this.FunctionPath.Append(par.Name);
                VariableInfo info;

                if (par.Type is VariableType varType) {
                    info = new VariableInfo() {
                        DefinitionKind = VariableDefinitionKind.Alias,
                        IsFunctionParameter = true,
                        Type = varType.InnerType
                    };
                }
                else {
                    info = new VariableInfo() {
                        DefinitionKind = VariableDefinitionKind.Local,
                        IsFunctionParameter = true,
                        Type = par.Type
                    };
                }

                cache.Variables.Add(path, info);
            }
        }

        public IEvaluateResult Evaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            return new AtomicEvaluateResult(0);
        }

        public void PreEvaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            memory[this.FunctionPath] = new AtomicEvaluateResult(this.FunctionBody);

            this.FunctionBody.PreEvaluate(memory);
        }

        public void ResolveNames(NameCache<NameTarget> names) {
            this.Signature.ReturnType = this.Signature.ReturnType.Resolve(names);

            names.PushLocalFrame();

            foreach (var par in this.Signature.Parameters) {
                // Resolve type names
                par.Type = par.Type.Resolve(names);

                // Get parameter path
                var parScope = this.FunctionPath.Append(par.Name);

                // Reserve parameter names
                names.AddLocalName(parScope, NameTarget.Variable);
            }

            // Resolve the body's names
            this.FunctionBody.ResolveNames(names);

            names.PopLocalFrame();
        }

        public void ResolveScope(IdentifierPath containingScope) {
            // This is the earliest possible time to check for duplicate parameter names
            var parNames = this.Signature.Parameters.Select(x => x.Name).ToArray();
            var unique = parNames.Distinct().ToArray();

            if (parNames.Length != unique.Length) {
                throw new Exception();
            }

            // Proceed as normal
            this.Scope = containingScope;
            this.FunctionPath = this.Scope.Append(this.Signature.Name);
            this.FunctionBody.ResolveScope(this.FunctionPath);
        }

        public ISyntax ResolveTypes(TypeChache types) {
            this.FunctionBody = this.FunctionBody.ResolveTypes(types);

            // Make sure the body type matches the return type
            if (this.FunctionBody.ReturnType != this.Signature.ReturnType) {
                throw new Exception();
            }

            this.ReturnType = VoidType.Instance;

            return this;
        }
    }
}
