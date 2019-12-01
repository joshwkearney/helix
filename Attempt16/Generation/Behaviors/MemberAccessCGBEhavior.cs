using Attempt16.Syntax;
using Attempt16.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt16.Generation {
    public class MemberAccessCGBehavior : ExpressionCodeGenerator<MemberAccessSyntax> {
        public MemberAccessCGBehavior(ISyntaxVisitor<IExpressionCodeGenerator> cg, TypeGenerator typeGen) : base(cg, typeGen) {
        }

        public override CCode Generate(MemberAccessSyntax syntax) {
            var writer = new CWriter();
            var value = syntax.Target.Accept(this.CodeGenerator).Generate(syntax.Target);
            var structType = (SingularStructType)((VariableType)syntax.Target.ReturnType).TargetType;

            writer.Append(value);

            string line;           
            
            if (value.Value.StartsWith("&")) {
                line = $"({value.Value.Substring(1)}.{syntax.MemberName})";
            }
            else {
                line = $"({value.Value}->{syntax.MemberName})";
            }

            if (structType.Members.First(x => x.Name == syntax.MemberName).TypePath.IsPathToVariable() && !syntax.IsLiteralAccess) {
                line = "(" + CWriter.Dereference(line) + ")";
            }

            return writer.ToCCode(line);
        }
    }
}
