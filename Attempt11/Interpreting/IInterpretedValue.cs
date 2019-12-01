using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt10 {
    public interface IInterpretedValue {
        ISyntaxTree GetSyntax(Scope scope);
    }

    public class VariableInterpretedValue : IInterpretedValue {
        private Func<Scope, VariableLiteralSyntax> factory;

        public IInterpretedValue Value { get; }

        public VariableInterpretedValue(IInterpretedValue value, Func<Scope, VariableLiteralSyntax> syntaxFactory) {
            this.Value = value;
            this.factory = syntaxFactory;
        }

        public VariableLiteralSyntax GetSyntax(Scope scope) => this.factory(scope);

        ISyntaxTree IInterpretedValue.GetSyntax(Scope scope) => this.GetSyntax(scope);
    }

    public class GenericFunction : IInterpretedValue {
        public FunctionTrophyType ClosureType { get; }

        public IReadOnlyList<string> ParameterNames { get; }

        public IInterpretedValue Body { get; }

        public FunctionTrophyType Type { get; }

        public GenericFunction(FunctionTrophyType type, IReadOnlyList<string> paramNames, IInterpretedValue body) {
            this.ClosureType = type;
            this.ParameterNames = paramNames;
            this.Body = body;
            this.Type = type;
        }

        public FunctionLiteralSyntax GenerateFunction(Scope scope, IReadOnlyList<ITrophyType> types) {
            if (types.Count != this.ClosureType.ParameterTypes.Count) {
                throw new Exception();
            }

            foreach (var (type1, type2) in types.Zip(this.ClosureType.ParameterTypes, (x, y) => (x, y))) {
                if (!type1.IsCompatibleWith(type2)) {
                    throw new Exception();
                }
            }            

            var newScope = scope;
            var pars = this.ParameterNames.Zip(types, (x, y) => new FunctionParameter(x, y)).ToArray();
            foreach (var par in pars) {
                var value = new SyntaxLiteral(new VariableLiteralSyntax(par.Name, par.Type, scope));
                newScope = newScope.SetVariable(par.Name, par.Type, value);
            }

            var body = this.Body.GetSyntax(newScope);
            var closedVars = new ClosureAnalyzer(body).Analyze()
                //.GetClosedVariables()
                .Where(x => !this.ParameterNames.Any(y => y == x.Key))
                .ToDictionary(x => x.Key, x => x.Value);

            return new FunctionLiteralSyntax(body, body.ExpressionType, closedVars, pars);
        }

        public FunctionLiteralSyntax GetSyntax(Scope scope) {
            return this.GenerateFunction(scope, this.ClosureType.ParameterTypes);
        }

        ISyntaxTree IInterpretedValue.GetSyntax(Scope scope) => this.GetSyntax(scope);
    }

    public class SyntaxLiteral : IInterpretedValue {
        private Func<Scope, ISyntaxTree> treeFactory;

        public SyntaxLiteral(ISyntaxTree value) : this(_ => value) { }

        public SyntaxLiteral(Func<Scope, ISyntaxTree> treeFactory) {
            this.treeFactory = treeFactory;
        }

        public ISyntaxTree GetSyntax(Scope scope) => this.treeFactory(scope);
    }
}