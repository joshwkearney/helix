using Attempt16.Syntax;
using Attempt16.Types;
using System.Collections.Generic;
using System.Linq;

namespace Attempt16.Generation {
    public class StructInitializationCGBehavior : ExpressionCodeGenerator<StructInitializationSyntax> {
        int tempCounter = 0;

        public StructInitializationCGBehavior(ISyntaxVisitor<IExpressionCodeGenerator> cg, TypeGenerator typeGen) : base(cg, typeGen) {
        }

        public override CCode Generate(StructInitializationSyntax syntax) {
            var writer = new CWriter();
            var implicitMemsWriter = new CWriter();
            var values = new Dictionary<string, string>();

            foreach (var mem in syntax.Members) {
                var code = mem.Value.Accept(this.CodeGenerator).Generate(mem.Value);
                writer.Append(code);

                if (mem.Operation == DeclarationOperation.Equate) {
                    values[mem.MemberName] = code.Value;
                }
                else {
                    string target = code.Value;

                    if (!target.StartsWith("_temp")) {
                        var type = mem.Value.ReturnType.Accept(this.TypeGenerator);
                        var name = "_temp_structmem_" + this.tempCounter++;

                        writer.Append(type);
                        implicitMemsWriter.VariableDeclaration(type.CTypeName, name, code.Value);

                        target = name;
                    }

                    values[mem.MemberName] = "&" + target;
                }
            }

            if (implicitMemsWriter.SourceCode.Any()) {
                implicitMemsWriter.SourceEmptyLine();
            }

            writer.Append(implicitMemsWriter);

            var structType = syntax.StructType.Accept(this.TypeGenerator);
            var tempName = "_temp_structinit_" + this.tempCounter++;

            writer.SourceLine(structType.CTypeName + " " + tempName + ";");
            writer.Append(structType);

            foreach (var (name, value) in values) {
                writer.SourceLine(tempName + "." + name + " = " + value + ";");
            }

            writer.SourceEmptyLine();

            return writer.ToCCode(tempName);
        }
    }
}