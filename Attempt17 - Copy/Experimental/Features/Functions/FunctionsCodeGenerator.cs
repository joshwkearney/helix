using Attempt17.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt17.Experimental.Features.Functions {
    public class FunctionsCodeGenerator {
        private int invokeTempCounter = 0;

        public CBlock GenerateFunctionDeclaration(FunctionDeclarationSyntax<TypeCheckInfo> syntax, ICodeGenerator gen) {
            var writer = new CWriter();
            var body = gen.Generate(syntax.Body);
            var line = this.GenerateSignature(syntax.Signature) + " {";

            writer.Line(line);
            writer.Lines(CWriter.Indent(body.SourceLines));
            writer.Lines(CWriter.Indent("return " + body.Value + ";"));
            writer.Line("}");
            writer.EmptyLine();

            gen.ForwardDeclarationsWriter
                .Line(this.GenerateSignature(syntax.Signature) + ";")
                .EmptyLine();

            return writer.ToBlock("0");
        }

        public CBlock GenerateInvoke(InvokeSyntax syntax, ICodeGenerator gen) {
            var args = syntax.Arguments.Select(gen.Generate).ToArray();
            var targetName = syntax.Target.Path.ToCName();
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

        private string GenerateSignature(FunctionSignature sig) {
            var line = "";

            line += sig.ReturnType.GenerateCType() + " ";
            line += sig.Name;
            line += "(";

            foreach (var par in sig.Parameters) {
                line += par.Type.GenerateCType() + " ";
                line += par.Name + ", ";
            }

            line = line.TrimEnd(' ', ',');
            line += ")";

            return line;
        }
    }
}