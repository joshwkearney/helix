using Attempt17.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.Features.Functions {
    public class FunctionDeclarationSyntaxTree : IDeclarationSyntaxTree {
        public FunctionSignature Signature { get; }

        public ISyntaxTree Body { get; }

        public FunctionDeclarationSyntaxTree(FunctionSignature sig, ISyntaxTree body) {
            this.Signature = sig;
            this.Body = body;
        }

        public ImmutableList<string> GenerateCode(CodeGenerator gen) {
            var writer = new CWriter();
            var body = this.Body.GenerateCode(gen);           
            var line = this.GenerateSignature() + " {";

            writer.Line(line);
            writer.Lines(CWriter.Indent(body.SourceLines));
            writer.Lines(CWriter.Indent("return " + body.Value + ";"));
            writer.Line("}");
            writer.EmptyLine();

            return writer.ToLines();
        }

        public void GenerateForwardDeclarations(CodeGenerator gen) {
            var writer = gen.GetHeaderWriter();

            writer.Line(this.GenerateSignature() + ";");
            writer.EmptyLine();
        }

        private string GenerateSignature() {
            var line = "";

            line += this.Signature.ReturnType.GenerateCType() + " ";
            line += this.Signature.Name;
            line += "(";

            foreach (var par in this.Signature.Parameters) {
                line += par.Type.GenerateCType() + " ";
                line += par.Name + ", ";
            }

            line = line.TrimEnd(' ', ',');
            line += ")";

            return line;
        }
    }
}