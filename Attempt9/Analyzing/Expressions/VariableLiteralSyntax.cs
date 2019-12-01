using System;
using System.Text;

namespace Attempt9 {

    public class VariableLiteralSyntax : IExpressionSyntax {
        public string Name { get; }

        public ITrophyType ReturnType { get; }

        public VariableLiteralSyntax(string name, ITrophyType type) {
            this.Name = name;
            this.ReturnType = type;
        }

        public void Accept(IExpressionVisitor visitor) => visitor.Visit(this);
    }
}