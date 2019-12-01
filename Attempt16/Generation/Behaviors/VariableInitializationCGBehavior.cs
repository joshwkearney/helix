using Attempt16.Syntax;
using Attempt16.Types;
using System;

namespace Attempt16.Generation {
    public class VariableInitializationCGBehavior : ExpressionCodeGenerator<VariableStatement> {
        public VariableInitializationCGBehavior(ISyntaxVisitor<IExpressionCodeGenerator> cg, TypeGenerator typeGen) : base(cg, typeGen) {
        }

        public override CCode Generate(VariableStatement syntax) {
            if (syntax.Operation == DeclarationOperation.Equate) {
                var writer = new CWriter();
                var ctype = syntax.ReturnType.Accept(this.TypeGenerator);
                var value = syntax.Value.Accept(this.CodeGenerator).Generate(syntax.Value);

                writer.Append(ctype);
                writer.Append(value);
                writer.VariableDeclaration(ctype.CTypeName + "*", syntax.VariableName, value.Value);
                writer.SourceEmptyLine();

                return writer.ToCCode(CWriter.Dereference(syntax.VariableName));
            }
            else if (syntax.Operation == DeclarationOperation.Store) {
                var writer = new CWriter();
                var ctype = syntax.ReturnType.Accept(this.TypeGenerator);
                var value = syntax.Value.Accept(this.CodeGenerator).Generate(syntax.Value);

                writer.Append(ctype);
                writer.Append(value);
                writer.VariableDeclaration(ctype.CTypeName, syntax.VariableName, value.Value);
                writer.SourceEmptyLine();

                return writer.ToCCode(syntax.VariableName);
            }
            else {
                throw new Exception();
            }
        }
    }
}
