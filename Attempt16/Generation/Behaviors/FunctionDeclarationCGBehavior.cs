using Attempt16.Syntax;
using Attempt16.Types;
using System.Linq;

namespace Attempt16.Generation {
    public class FunctionDeclarationCGBehavior : DeclarationCodeGenerator<FunctionDeclaration> {
        public FunctionDeclarationCGBehavior(ISyntaxVisitor<IExpressionCodeGenerator> cg, TypeGenerator typeGen) : base(cg, typeGen) {
        }

        public override CCode Generate(FunctionDeclaration decl) {
            var writer = new CWriter();
            var retType = this.TypeGenerator.VisitIdenfitierPath(decl.ReturnType);
            var funcType = decl.FunctionType.Accept(this.TypeGenerator);

            writer.Append(funcType).Append(retType);

            string line = "";

            line += retType.CTypeName;
            line += " ";
            line += decl.Name;
            line += "(";

            foreach (var par in decl.Parameters) {
                var type = this.TypeGenerator.VisitIdenfitierPath(par.TypePath);

                writer.Append(type);

                line += type.CTypeName;
                line += " ";
                line += par.Name;
                line += ", ";
            }

            line = line.Trim(' ', ',');
            line += ") {";

            writer.SourceLine(line);

            var body = decl.Body.Accept(this.CodeGenerator).Generate(decl.Body);

            writer.SourceLines(CWriter.Indent(body.SourceLines));
            writer.SourceLines(CWriter.Indent("return " + body.Value + ";"));
            writer.SourceLine("}");
            writer.SourceEmptyLine();
            writer.HeaderLines(body.HeaderLines);

            return writer.ToCCode(null);
        }
    }
}