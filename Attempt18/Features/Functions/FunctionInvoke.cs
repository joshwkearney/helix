using System;
using System.Collections.Generic;
using System.Linq;
using Attempt19.Evaluation;
using Attempt19.Types;

namespace Attempt19.Features.Functions {
    public class FunctionInvoke : ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public ISyntax[] Arguments { get; set; }

        public ISyntax Target { get; set; }

        public IdentifierPath TargetPath { get; set; }

        public FunctionSignature TargetSignature { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            this.Target.AnalyzeFlow(types, flow);

            foreach (var arg in this.Arguments) {
                arg.AnalyzeFlow(types, flow);
            }

            var mutators = this.ReturnType.GetMutators(types).Add(this.ReturnType);
            var captured = new List<IdentifierPath>();

            if (this.ReturnType.GetCopiability(types) != Copiability.None) {
                foreach (var arg in this.Arguments) {
                    captured.AddRange(arg.CapturedVariables);
                }
            }

            this.CapturedVariables = captured.ToArray();
        }

        public void DeclareNames(NameCache<NameTarget> names) {
            this.Target.DeclareNames(names);

            foreach (var arg in this.Arguments) {
                arg.DeclareNames(names);
            }
        }

        public void DeclareTypes(TypeChache cache) {
            this.Target.DeclareTypes(cache);

            foreach (var arg in this.Arguments) {
                arg.DeclareTypes(cache);
            }
        }

        public IEvaluateResult Evaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            // Evaulate arguments
            var args = this.Arguments.Select(x => x.Evaluate(memory)).ToArray();

            // Evaluate target
            var target = (ISyntax)this.Target.Evaluate(memory).Value;

            // Save previous parameter values
            var prev = new Dictionary<IdentifierPath, IEvaluateResult>();

            foreach (var par in this.TargetSignature.Parameters) {
                var path = this.TargetPath.Append(par.Name);

                if (memory.TryGetValue(path, out var value)) {
                    prev.Add(path, value);
                }
            }

            // Load arguments
            for (int i = 0; i < this.Arguments.Length; i++) {
                var path = this.TargetPath.Append(this.TargetSignature.Parameters[i].Name);

                memory[path] = args[i];
            }

            // Evaluate body
            var result = target.Evaluate(memory);

            // Restore old values
            foreach (var pair in prev) {
                memory[pair.Key] = pair.Value;
            }

            return result;
        }

        public void PreEvaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            this.Target.PreEvaluate(memory);

            foreach (var arg in this.Arguments) {
                arg.PreEvaluate(memory);
            }
        }

        public void ResolveNames(NameCache<NameTarget> names) {
            this.Target.ResolveNames(names);

            foreach (var arg in this.Arguments) {
                arg.ResolveNames(names);
            }
        }

        public void ResolveScope(IdentifierPath containingScope) {
            this.Scope = containingScope;

            this.Target.ResolveScope(containingScope);

            foreach (var arg in this.Arguments) {
                arg.ResolveScope(containingScope);
            }
        }

        public ISyntax ResolveTypes(TypeChache types) {
            this.Target.ResolveTypes(types);
            this.Arguments = this.Arguments.Select(x => x.ResolveTypes(types)).ToArray();

            if (!(this.Target.ReturnType is FunctionType funcType)) {
                throw new Exception("Cannot invoke non-function type");
            }

            var sig = types.Functions[funcType.FunctionPath];

            if (this.Arguments.Length != sig.Parameters.Length) {
                throw new Exception("Argument and parameter counts must match");
            }

            for (int i = 0; i < this.Arguments.Length; i++) {
                if (this.Arguments[i].ReturnType != sig.Parameters[i].Type) {
                    throw new Exception();
                }
            }

            this.ReturnType = sig.ReturnType;
            this.TargetSignature = sig;
            this.TargetPath = funcType.FunctionPath;

            return this;
        }
    }
}