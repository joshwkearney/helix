using Attempt17.CodeGeneration;
using Attempt17.TypeChecking;
using System.Linq;

namespace Attempt17.Features.Functions {
    public class FunctionsCodeGenerator {
        private int invokeTempCounter = 0;

        public CBlock GenerateFunctionDeclaration(FunctionDeclarationSyntax syntax, ICodeGenerator gen) {
            var writer = new CWriter();
            var body = gen.Generate(syntax.Body);
            var line = this.GenerateSignature(syntax.Info, gen) + " {";

            writer.Line(line);
            writer.Lines(CWriter.Indent(body.SourceLines));
            writer.Lines(CWriter.Indent("return " + body.Value + ";"));
            writer.Line("}");
            writer.EmptyLine();

            gen.Header1Writer
                .Line("typedef short " + syntax.Info.Path.ToCName() + ";")
                .EmptyLine();

            gen.Header2Writer
                .Line(this.GenerateSignature(syntax.Info, gen) + ";")
                .EmptyLine();

            return writer.ToBlock("0");
        }

        public CBlock GenerateInvoke(InvokeSyntax syntax, ICodeGenerator gen) {
            var args = syntax.Arguments.Select(gen.Generate).ToArray();
            var targetName = "$func$" + syntax.Target.Path.ToCName();
            var tempName = "$invoke_result_" + this.invokeTempCounter++;
            var tempType = gen.Generate(syntax.Tag.ReturnType);
            var writer = new CWriter();
            var invoke = targetName + "(";

            foreach (var arg in args) {
                invoke += arg.Value + ", ";
                writer.Lines(arg.SourceLines);
            }

            invoke = invoke.TrimEnd(',', ' ');
            invoke += ")";

            writer.VariableInit(tempType, tempName, invoke);

            return writer.ToBlock(tempName);
        }

        public CBlock GenerateFunctionLiteral(FunctionLiteralSyntax syntax, ICodeGenerator gen) {
            return new CBlock("0");
        }

        private string GenerateSignature(FunctionInfo info, ICodeGenerator gen) {
            var line = "";

            line += gen.Generate(info.Signature.ReturnType) + " ";
            line += "$func$" + info.Path.ToCName();
            line += "(";

            foreach (var par in info.Signature.Parameters) {
                line += gen.Generate(par.Type) + " ";
                line += par.Name + ", ";
            }

            line = line.TrimEnd(' ', ',');
            line += ")";

            return line;
        }
    }
}