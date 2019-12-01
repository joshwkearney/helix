using System.Collections.Generic;
using System.Linq;

namespace Attempt10 {
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

        public ITrophyType ExpressionType { get; }

        public ITrophyType ReturnType { get; }

        public IReadOnlyDictionary<string, ITrophyType> ClosedVariables { get; }

        ITrophyType ISyntaxTree.ExpressionType => this.ExpressionType;

        public FunctionLiteralSyntax(
            ISyntaxTree body, 
            ITrophyType returnType,
            IReadOnlyDictionary<string, ITrophyType> closedVars,
            IReadOnlyList<FunctionParameter> pars) {

            this.Body = body;
            this.Parameters = pars;
            this.ClosedVariables = closedVars;
            this.ReturnType = returnType;

            this.ExpressionType = new ClosureTrophyType(new FunctionTrophyType(returnType, pars.Select(x => x.Type).ToArray()));
        }

        public void Accept(ISyntaxTreeVisitor visitor) => visitor.Visit(this);
    }
}