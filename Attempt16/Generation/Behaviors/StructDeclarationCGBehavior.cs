using Attempt16.Syntax;
using Attempt16.Types;

namespace Attempt16.Generation {
    public class StructDeclarationCGBehavior : DeclarationCodeGenerator<StructDeclaration> {
        public StructDeclarationCGBehavior(ISyntaxVisitor<IExpressionCodeGenerator> cg, TypeGenerator typeGen) : base(cg, typeGen) {
        }

        public override CCode Generate(StructDeclaration declaration) {
            var writer = new CWriter();

            writer.Append(declaration.StructType.Accept(this.TypeGenerator));

            return writer.ToCCode(null);
        }
    }
}
