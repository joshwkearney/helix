using System.Collections.Generic;
using System.Linq;

namespace Attempt12 {
    public class FunctionParameter {
        public ITrophyType Type { get; }

        public string Name { get; }

        public FunctionParameter(string name, ITrophyType type) {
            this.Name = name;
            this.Type = type;
        }
    }

    public class FunctionLiteralSyntax : ISyntaxTree {
        public IReadOnlyList<FunctionParameter> Parameters { get; }

        public ISyntaxTree Body { get; }

        public TrophyFunctionType ExpressionType { get; }

        public ITrophyType ReturnType => this.ExpressionType.ReturnType;

        public Scope Scope { get; }

        public IReadOnlyList<VariableInfo> ClosedVariables { get; }

        ITrophyType ISyntaxTree.ExpressionType => this.ExpressionType;

        public bool IsConstant { get; }

        public FunctionLiteralSyntax(
            TrophyFunctionType type,
            ISyntaxTree body, 
            Scope env, 
            IReadOnlyList<VariableInfo> closedVars,
            IReadOnlyList<FunctionParameter> pars) {

            this.Body = body;
            this.Parameters = pars;
            this.Scope = env;
            this.ClosedVariables = closedVars;
            this.ExpressionType = type;

            this.IsConstant = this.ClosedVariables.Count == 0 && this.ReturnType.IsLiteral;
        }

        public void Accept(ISyntaxTreeVisitor visitor) => visitor.Visit(this);
    }
}