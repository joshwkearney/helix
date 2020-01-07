using Attempt17.CodeGeneration;
using Attempt17.TypeChecking;
using System;

namespace Attempt17.Features.Primitives {
    public class PrimitivesCodeGenerator {
        public CBlock GenerateIntLiteral(IntLiteralSyntax<TypeCheckTag> literal, ICodeGenerator gen) {
            return new CBlock(literal.Value.ToString() + "LL");
        }

        public CBlock GenerateVoidLiteral(VoidLiteralSyntax<TypeCheckTag> literal, ICodeGenerator gen) {
            return new CBlock("0");
        }

        public CBlock GenerateBinarySyntax(BinarySyntax<TypeCheckTag> syntax, ICodeGenerator gen) {
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