using Attempt17.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Experimental.Features.Primitives {
    public class PrimitivesCodeGenerator {
        public CBlock GenerateIntLiteral(IntLiteralSyntax<TypeCheckInfo> literal, ICodeGenerator gen) {
            return new CBlock(literal.Value.ToString() + "LL");
        }

        public CBlock GenerateVoidLiteral(VoidLiteralSyntax<TypeCheckInfo> literal, ICodeGenerator gen) {
            return new CBlock("0");
        }

        public CBlock GenerateBinarySyntax(BinarySyntax<TypeCheckInfo> syntax, ICodeGenerator gen) {
            var op = syntax.Kind switch {
                BinarySyntaxKind.Add => " + ",
                BinarySyntaxKind.Subtract => " - ",
                BinarySyntaxKind.Multiply => " * ",
                _ => throw new Exception("This should never happen"),
            };

            var left = gen.Generate(syntax.Left);
            var right = gen.Generate(syntax.Right);

            return left.Combine(right, (x, y) => "(" + x + op + y + ")");
        }
    }
}