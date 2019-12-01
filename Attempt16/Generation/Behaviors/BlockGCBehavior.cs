using Attempt16.Syntax;
using Attempt16.Types;
using System.Linq;

namespace Attempt16.Generation {
    public class BlockGCBehavior : ExpressionCodeGenerator<BlockSyntax> {
        public BlockGCBehavior(ISyntaxVisitor<IExpressionCodeGenerator> cg, TypeGenerator typeGen) : base(cg, typeGen) {
        }

        public override CCode Generate(BlockSyntax syntax) {
            if (syntax.Statements.Any()) {
                var codes = syntax.Statements.Select(x => x.Accept(this.CodeGenerator).Generate(x)).ToArray();

                return codes.Aggregate(new CWriter(), (x, y) => x.Append(y)).ToCCode(codes.Last().Value);
            }
            else {
                return new CCode("0");
            }
        }
    }
}
