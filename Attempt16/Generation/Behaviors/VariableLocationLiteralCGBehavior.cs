using Attempt16.Syntax;
using Attempt16.Types;

namespace Attempt16.Generation {
    public class VariableLocationLiteralCGBehavior : ExpressionCodeGenerator<VariableLocationLiteral> {
        public VariableLocationLiteralCGBehavior(ISyntaxVisitor<IExpressionCodeGenerator> cg, TypeGenerator typeGen) : base(cg, typeGen) {
        }

        public override CCode Generate(VariableLocationLiteral syntax) {
            if (syntax.Source == VariableSource.Local) {
                return new CCode("&" + syntax.VariableName);
            }
            else {
                return new CCode(syntax.VariableName);
            }
        }
    }
}
