using System;
using System.Collections.Immutable;
using System.Linq;
using Attempt17.CodeGeneration;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features.Functions {
    public class FunctionsCodeGenerator
        : IFunctionsVisitor<CBlock, TypeCheckTag, CodeGenerationContext> {

        private int invokeTempCounter = 0;

        public CBlock VisitFunctionDeclaration(FunctionDeclarationSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            // Get a new scope
            var scope = new FunctionCScope(context.Scope);
            context = context.WithScope(scope);

            // Add the parameters to the scope
            foreach (var par in syntax.FunctionInfo.Signature.Parameters) {
                scope.SetVariableUndestructed(par.Name, par.Type);
            }

            var writer = new CWriter();
            var body = syntax.Body.Accept(visitor, context);
            var returnType = context.Generator.Generate(syntax.FunctionInfo.Signature.ReturnType);

            var line = GenerateSignature(syntax.FunctionInfo.Signature,
                syntax.FunctionInfo.Path, context) + " {";

            var varsToCleanUp = scope
                .GetUndestructedVariables()
                .ToImmutableDictionary(x => x.Key, x => x.Value);

            writer.Line(line);
            writer.Lines(CWriter.Indent(body.SourceLines));
            writer.Lines(CWriter.Indent("// Function cleanup"));
            writer.Lines(CWriter.Indent($"{returnType} $func_return = {body.Value};"));
            writer.Lines(CWriter.Indent(ScopeHelper.CleanupScope(varsToCleanUp,
                context.Generator)));
            writer.Lines(CWriter.Indent("return $func_return;"));
            writer.Line("}");
            writer.EmptyLine();

            context
                .Generator
                .Header2Writer
                .Line(GenerateSignature(
                    syntax.FunctionInfo.Signature, syntax.FunctionInfo.Path, context) + ";")
                .EmptyLine();

            return writer.ToBlock("0");
        }

        public CBlock VisitFunctionLiteral(FunctionLiteralSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            return new CBlock("0");
        }

        public CBlock VisitInvoke(InvokeSyntax<TypeCheckTag> syntax,
            ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor,
            CodeGenerationContext context) {

            var args = syntax.Arguments.Select(x => x.Accept(visitor, context)).ToArray();
            var funcType = (NamedType)syntax.Target.Tag.ReturnType;
            var targetName = funcType.Path.ToCName();
            var tempName = "$invoke_result_" + this.invokeTempCounter++;
            var tempType = context.Generator.Generate(syntax.Tag.ReturnType);
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

            context.Scope.SetVariableUndestructed(tempName, syntax.Tag.ReturnType);

            return writer.ToBlock(tempName);
        }

        public static string GenerateSignature(FunctionSignature sig, IdentifierPath path,
            CodeGenerationContext context) {

            var line = "";

            line += context.Generator.Generate(sig.ReturnType) + " ";
            line += path.ToCName();
            line += "(";

            foreach (var par in sig.Parameters) {
                line += context.Generator.Generate(par.Type) + " ";
                line += par.Name + ", ";
            }

            line = line.TrimEnd(' ', ',');
            line += ")";

            return line;
        }
    }
}
