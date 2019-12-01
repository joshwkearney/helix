using Attempt16.Syntax;
using Attempt16.Types;

namespace Attempt16.Generation {
    public class IfGCBehavior : ExpressionCodeGenerator<IfSyntax> {
        private int tempCounter = 0;

        public IfGCBehavior(ISyntaxVisitor<IExpressionCodeGenerator> cg, TypeGenerator typeGen) : base(cg, typeGen) {
        }

        public override CCode Generate(IfSyntax syntax) {
            string resultValue = syntax.Negative == null ? "0" : "_temp_if_" + this.tempCounter++;

            var cond = syntax.Condition.Accept(this.CodeGenerator).Generate(syntax.Condition);
            var affirm = syntax.Affirmative.Accept(this.CodeGenerator).Generate(syntax.Affirmative);

            var affirmWriter = new CWriter(affirm);
            var negWriter = new CWriter();

            if (syntax.Negative != null) {
                affirmWriter.Assignment(resultValue, affirm.Value);

                var neg = syntax.Negative.Accept(this.CodeGenerator).Generate(syntax.Negative);

                negWriter.Append(neg);
                negWriter.Assignment(resultValue, neg.Value);
            }

            var affirmCode = affirmWriter.ToCCode(resultValue);
            var negCode = negWriter.ToCCode(resultValue);
            var writer = new CWriter();

            writer.SourceLine($"if ({cond.Value}) {{");
            writer.SourceLines(CWriter.Indent(affirmCode.SourceLines));
            writer.SourceLine("}");
            writer.HeaderLines(affirmCode.HeaderLines);

            if (syntax.Negative != null) {
                writer.SourceLine("else {");
                writer.SourceLines(CWriter.Indent(negCode.SourceLines));
                writer.SourceLine("}");
                writer.HeaderLines(negCode.HeaderLines);
            }

            writer.SourceEmptyLine();

            return writer.ToCCode(resultValue);
        }
    }
}
