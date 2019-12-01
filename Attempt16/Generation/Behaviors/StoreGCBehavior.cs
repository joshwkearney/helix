using Attempt16.Syntax;
using Attempt16.Types;

namespace Attempt16.Generation {
    public class StoreGCBehavior : ExpressionCodeGenerator<StoreSyntax> {
        public StoreGCBehavior(ISyntaxVisitor<IExpressionCodeGenerator> cg, TypeGenerator typeGen) : base(cg, typeGen) {
        }

        public override CCode Generate(StoreSyntax syntax) {
            var target = syntax.Target.Accept(this.CodeGenerator).Generate(syntax.Target);
            var value = syntax.Value.Accept(this.CodeGenerator).Generate(syntax.Value);

            var writer = new CWriter();

            writer.Append(target);
            writer.Append(value);

            string lhs = CWriter.Dereference(target.Value);

            writer.Assignment(lhs, value.Value);
            writer.SourceEmptyLine();

            return new CCode(
                "0",
                writer.SourceCode,
                target.HeaderLines.AddRange(value.HeaderLines)
            );
        }
    }
}
