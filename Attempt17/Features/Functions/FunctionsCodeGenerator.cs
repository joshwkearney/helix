using Attempt17.CodeGeneration;
using Attempt17.TypeChecking;
using Attempt17.Types;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt17.Features.Functions {
    public class FunctionsCodeGenerator {
        private int invokeTempCounter = 0;

        public CBlock GenerateFunctionDeclaration(FunctionDeclarationSyntax syntax, ICScope scope, ICodeGenerator gen) {
            scope = new FunctionCScope();

            // Add the parameters to the scope
            foreach (var par in syntax.Info.Signature.Parameters) {
                scope.SetVariableUndestructed(par.Name, par.Type);
            }

            var writer = new CWriter();
            var body = gen.Generate(syntax.Body, scope);
            var returnType = gen.Generate(syntax.Info.Signature.ReturnType);
            var line = this.GenerateSignature(syntax.Info, gen) + " {";
            var varsToCleanUp = scope
                .GetUndestructedVariables()
                .ToImmutableDictionary(x => x.Key, x => x.Value);

            writer.Line(line);
            writer.Lines(CWriter.Indent(body.SourceLines));
            writer.Lines(CWriter.Indent("// Function cleanup"));
            writer.Lines(CWriter.Indent($"{returnType} $func_return = {body.Value};"));
            writer.Lines(CWriter.Indent(ScopeHelper.CleanupScope(varsToCleanUp, gen)));
            writer.Lines(CWriter.Indent("return $func_return;"));
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

        public CBlock GenerateInvoke(InvokeSyntax syntax, ICScope scope, ICodeGenerator gen) {
            var args = syntax.Arguments.Select(x => gen.Generate(x, scope)).ToArray();
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

            writer.Line("// Function invoke");
            writer.VariableInit(tempType, tempName, invoke);
            writer.EmptyLine();

            scope.SetVariableUndestructed(tempName, syntax.Target.Signature.ReturnType);

            return writer.ToBlock(tempName);
        }

        public CBlock GenerateFunctionLiteral(FunctionLiteralSyntax syntax, ICScope scope, ICodeGenerator gen) {
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