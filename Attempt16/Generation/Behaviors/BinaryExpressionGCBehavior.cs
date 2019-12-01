using Attempt16.Syntax;
using Attempt16.Types;

namespace Attempt16.Generation {
    public class BinaryExpressionGCBehavior : ExpressionCodeGenerator<BinaryExpression> {
        public BinaryExpressionGCBehavior(ISyntaxVisitor<IExpressionCodeGenerator> cg, TypeGenerator typeGen) : base(cg, typeGen) {
        }

        public override CCode Generate(BinaryExpression syntax) {
            var left = syntax.Left.Accept(this.CodeGenerator).Generate(syntax.Left);
            var right = syntax.Right.Accept(this.CodeGenerator).Generate(syntax.Right);

            string result;

            switch (syntax.Operation) {
                case BinaryOperator.Add:
                    result = $"({left.Value} + {right.Value})";
                    break;
                case BinaryOperator.Subtract:
                    result = $"({left.Value} - {right.Value})";
                    break;
                case BinaryOperator.Multiply:
                    result = $"({left.Value} * {right.Value})";
                    break;
                default:
                    throw new System.Exception();
            }

            return new CWriter().Append(left).Append(right).ToCCode(result);
        }
    }
}
