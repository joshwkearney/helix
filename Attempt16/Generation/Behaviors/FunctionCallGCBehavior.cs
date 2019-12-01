using Attempt16.Syntax;
using Attempt16.Types;
using System.Linq;

namespace Attempt16.Generation {
    public class FunctionCallGCBehavior : ExpressionCodeGenerator<FunctionCallSyntax> {
        private int tempCounter = 0;

        public FunctionCallGCBehavior(ISyntaxVisitor<IExpressionCodeGenerator> cg, TypeGenerator typeGen) : base(cg, typeGen) {
        }

        public override CCode Generate(FunctionCallSyntax syntax) {
            var funcType = (SingularFunctionType)syntax.Target.ReturnType;

            var name = funcType.Accept(this.TypeGenerator);
            string tempName = "_temp_call_" + this.tempCounter++;

            var writer = new CWriter().Append(name);
            var argCodes = syntax.Arguments.Select(x => x.Accept(this.CodeGenerator).Generate(x)).ToArray();

            argCodes.Aggregate(writer, (x, y) => x.Append(y));

            string line = string.Join(", ", argCodes.Select(x => x.Value));

            line = line.Trim(' ', ',');
            line = "(" + line + ")";
            line = name.CTypeName + line;

            var tempType = syntax.ReturnType.Accept(this.TypeGenerator);

            writer.Append(tempType);
            writer.VariableDeclaration(tempType.CTypeName, tempName, line);
            writer.SourceEmptyLine();

            return writer.ToCCode(tempName);
        }
    }
}
