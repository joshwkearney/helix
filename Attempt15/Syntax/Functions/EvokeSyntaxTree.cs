using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;

namespace JoshuaKearney.Attempt15.Syntax.Functions {
    public class EvokeSyntaxTree : ISyntaxTree {
        public ITrophyType ExpressionType => this.TargetType.ReturnType;

        public ExternalVariablesCollection ExternalVariables { get; }

        public IReadOnlyList<ISyntaxTree> Arguments { get; }

        public IFunctionType TargetType { get; }

        public EvokeSyntaxTree(IFunctionType targetType, IEnumerable<ISyntaxTree> args) {
            this.TargetType = targetType;
            this.Arguments = args.ToArray();

            this.ExternalVariables = args.Aggregate(
                new ExternalVariablesCollection(), 
                (x, y) => x.Union(y.ExternalVariables)
            );
        }

        public string GenerateCode(CodeGenerateEventArgs args) => args.FunctionGenerator.GenerateEvoke(this, args);

        public bool DoesVariableEscape(string variableName) {
            // TODO - Change this if adding side affects to functions
            return false;
        }
    }
}