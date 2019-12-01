using Attempt16.Syntax;
using Attempt16.Types;

namespace Attempt16.Generation {
    public class ValueofCGBehavior : ExpressionCodeGenerator<ValueofSyntax> {
        public ValueofCGBehavior(ISyntaxVisitor<IExpressionCodeGenerator> cg, TypeGenerator typeGen) : base(cg, typeGen) {
        }

        public override CCode Generate(ValueofSyntax syntax) {
            var value = syntax.Value.Accept(this.CodeGenerator).Generate(syntax.Value);

            return new CWriter(value).ToCCode(CWriter.Dereference(value.Value));
        }
    }
}
