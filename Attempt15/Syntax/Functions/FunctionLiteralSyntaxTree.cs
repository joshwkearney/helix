using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;

namespace JoshuaKearney.Attempt15.Syntax.Functions {
    public class FunctionLiteralSyntaxTree : ISyntaxTree {
        public FunctionType ExpressionType { get; }

        ITrophyType ISyntaxTree.ExpressionType => this.ExpressionType;

        public ExternalVariablesCollection ExternalVariables { get; }

        public ISyntaxTree Body { get; }

        public IReadOnlyList<IdentifierInfo> Parameters { get; }

        public FunctionLiteralSyntaxTree(ISyntaxTree body, IEnumerable<IdentifierInfo> pars) {
            this.Body = body;
            this.Parameters = pars.ToArray();
            this.ExternalVariables = body.ExternalVariables
                .Except(pars.Select(x => x.Name));

            this.ExpressionType = new FunctionType(
                body.ExpressionType,
                pars.Select(x => x.Type),
                body.ExternalVariables.Except(pars.Select(x => x.Name)).VariableInfos
            );
        }

        public string GenerateCode(CodeGenerateEventArgs args) {
            return args.FunctionGenerator.GenerateFunctionLiteral(this, args);
        }

        public bool DoesVariableEscape(string variableName) {
            return this.ExternalVariables.VariableNames.Contains(variableName);
        }
    }
}