using Attempt17.CodeGeneration;
using Attempt17.TypeChecking;
using System.Linq;

namespace Attempt17.Features.FlowControl {
    public class FlowControlCodeGenerator {
        private int tempCounter = 0;

        public CBlock GenerateWhileSyntax(WhileSyntax<TypeCheckTag> syntax, ICodeGenerator gen) {
            var cond = gen.Generate(syntax.Condition);
            var body = gen.Generate(syntax.Body);
            var writer = new CWriter();

            writer.Lines(cond.SourceLines);
            writer.Line($"while ({cond.Value}) {{");
            writer.Lines(CWriter.Trim(CWriter.Indent(body.SourceLines)));
            writer.Line("}");
            writer.EmptyLine();

            return writer.ToBlock("0");
        }

        public CBlock GenerateIfSyntax(IfSyntax<TypeCheckTag> syntax, ICodeGenerator gen) {
            var writer = new CWriter();
            var cond = gen.Generate(syntax.Condition);
            var affirm = gen.Generate(syntax.Affirmative);
            var neg = syntax.Negative.Select(x => gen.Generate(x));

            writer.Lines(cond.SourceLines);

            if (syntax.Kind == IfKind.Expression) {
                var tempType = gen.Generate(syntax.Affirmative.Tag.ReturnType);
                var tempName = "$if_result_" + this.tempCounter++;

                writer.VariableInit(tempType, tempName);
                writer.Line($"if ({cond.Value}) {{");
                writer.Lines(CWriter.Indent(affirm.SourceLines));
                writer.Line($"    {tempName} = {affirm.Value};");
                writer.Line("}");
                writer.Line("else {");
                writer.Lines(CWriter.Indent(neg.GetValue().SourceLines));
                writer.Line($"    {tempName} = {neg.GetValue().Value};");
                writer.Line("}");
                writer.EmptyLine();

                return writer.ToBlock(tempName);
            }
            else {
                writer.Line($"if ({cond.Value}) {{");
                writer.Lines(CWriter.Trim(CWriter.Indent(affirm.SourceLines)));
                writer.Line("}");

                if (neg.Any()) {
                    writer.Line("else {");
                    writer.Lines(CWriter.Trim(CWriter.Indent(neg.GetValue().SourceLines)));
                    writer.Line("}");
                }

                writer.EmptyLine();

                return writer.ToBlock("0");
            }
        }

        public CBlock GenerateBlockSyntax(BlockSyntax<TypeCheckTag> syntax, ICodeGenerator gen) {
            var stats = syntax.Statements.Select(gen.Generate).ToArray();

            if (stats.Any()) {
                return stats.Aggregate((x, y) => x.Combine(y, (c, v) => v));
            }
            else {
                return new CBlock("0");
            }
        }
    }
}
