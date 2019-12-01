using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;

namespace JoshuaKearney.Attempt15.Syntax.Functions {
    public class FunctionCallSyntaxTree : ISyntaxTree {
        public ISyntaxTree Target { get; }

        public IReadOnlyList<ISyntaxTree> Arguments { get; }

        public ITrophyType ExpressionType { get; }

        public ExternalVariablesCollection ExternalVariables { get; }

        public FunctionCallSyntaxTree(ISyntaxTree target, IFunctionType targetType, IEnumerable<ISyntaxTree> args) {
            this.Target = target;
            this.ExpressionType = targetType.ReturnType;
            this.Arguments = args.ToArray();
            this.ExternalVariables = args.Aggregate(target.ExternalVariables, (x, y) => x.Union(y.ExternalVariables));
        }

        public string GenerateCode(CodeGenerateEventArgs args) => args.FunctionGenerator.GenerateFunctionCall(this, args);

        public bool DoesVariableEscape(string variableName) {
            // TODO - Change this when adding side effects to functions
            return false;
        }
    }
}
