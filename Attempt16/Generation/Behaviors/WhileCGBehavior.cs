using Attempt16.Syntax;
using Attempt16.Types;

namespace Attempt16.Generation {
    public class WhileCGBehavior : ExpressionCodeGenerator<WhileStatement> {
        public WhileCGBehavior(ISyntaxVisitor<IExpressionCodeGenerator> cg, TypeGenerator typeGen) : base(cg, typeGen) {
        }

        public override CCode Generate(WhileStatement syntax) {
            var cond = syntax.Condition.Accept(this.CodeGenerator).Generate(syntax.Condition);
            var body = syntax.Body.Accept(this.CodeGenerator).Generate(syntax.Body);
            var writer = new CWriter(cond);

            writer.SourceLine($"while ({cond.Value}) {{");
            writer.SourceEmptyLine();
            writer.HeaderLines(body.HeaderLines);
            writer.SourceLines(CWriter.Trim(CWriter.Indent(body.SourceLines)));
            writer.SourceLine("}");
            writer.SourceEmptyLine();

            return writer.ToCCode("0");
        }
    }
}
