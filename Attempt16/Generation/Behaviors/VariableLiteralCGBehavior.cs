using Attempt16.Syntax;
using Attempt16.Types;

namespace Attempt16.Generation {
    public class VariableLiteralCGBehavior : ExpressionCodeGenerator<VariableLiteral> {
        public VariableLiteralCGBehavior(ISyntaxVisitor<IExpressionCodeGenerator> cg, TypeGenerator typeGen) : base(cg, typeGen) {
        }

        public override CCode Generate(VariableLiteral syntax) {
            if (syntax.Source == VariableSource.Local || syntax.Source == VariableSource.ValueParameter) {
                return new CCode(syntax.VariableName);
            }
            else {
                return new CCode("*" + syntax.VariableName);
            }
        }
    }
}
