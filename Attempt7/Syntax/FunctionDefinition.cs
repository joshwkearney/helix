using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt7.Syntax {
    public class FunctionParameterDefinition {
        public string TypeName { get; }

        public string VariableName { get; }

        public FunctionParameterDefinition(string typeName, string variableName) {
            this.TypeName = typeName;
            this.VariableName = variableName;
        }
    }

    public class FunctionDefinition : ISymbol {
        public IReadOnlyList<FunctionParameterDefinition> Parameters { get; }

        public ISymbol Body { get; }

        public FunctionDefinition(ISymbol body, IReadOnlyList<FunctionParameterDefinition> pars) {
            this.Body = body;
            this.Parameters = pars;
        }

        public CompilationResult Compile(BuilderArgs compiler, Scope scope) {
            throw new NotImplementedException();
        }

        public InterpretationResult Interpret(Scope scope) {
            var body = this.Body.Interpret(scope);
            var paramTypes = this.Parameters.Select(x => (LanguageType)scope.Variables[x.TypeName]).ToArray();
            var funcType = new ClosureType(body.ReturnType, paramTypes);

            ISymbol closure(IReadOnlyList<ISymbol> args) {
                if (args.Count != paramTypes.Length) {
                    throw new Exception();
                }

                Scope funcScope = Scope.GlobalScope;
                var vars = this.Parameters.Select(x => x.VariableName).Zip(args, (x, y) => new { Name = x, Value = y });

                foreach (var item in vars) {
                    funcScope = funcScope.WithVariable(item.Name, item.Value);
                }

                return this.Body.Interpret(funcScope).Result;
            }

            return new InterpretationResult((Closure)closure, scope, funcType);
        }
    }
}