using Attempt17.CodeGeneration;
using Attempt17.TypeChecking;
using Attempt17.Types;
using System;

namespace Attempt17.Features.Primitives {
    public class PrimitivesCodeGenerator {
        private int allocTempCounter = 0;

        public CBlock GenerateIntLiteral(IntLiteralSyntax<TypeCheckTag> literal, ICScope scope, ICodeGenerator gen) {
            return new CBlock(literal.Value.ToString() + "LL");
        }

        public CBlock GenerateVoidLiteral(VoidLiteralSyntax<TypeCheckTag> literal, ICScope scope, ICodeGenerator gen) {
            return new CBlock("0");
        }

        public CBlock GenerateBoolLiteral(BoolLiteralSyntax<TypeCheckTag> literal, ICScope scope, ICodeGenerator gen) {
            return new CBlock(literal.Value ? "1" : "0");
        }

        public CBlock GenerateBinarySyntax(BinarySyntax<TypeCheckTag> syntax, ICScope scope, ICodeGenerator gen) {
            string op;

            if (syntax.Left.Tag.ReturnType == IntType.Instance) {
                op = syntax.Kind switch {
                    BinarySyntaxKind.Add => " + ",
                    BinarySyntaxKind.Subtract => " - ",
                    BinarySyntaxKind.Multiply => " * ",
                    BinarySyntaxKind.And => " & ",
                    BinarySyntaxKind.Or => " | ",
                    BinarySyntaxKind.Xor => " ^ ",
                    BinarySyntaxKind.GreaterThan => " > ",
                    BinarySyntaxKind.LessThan => " < ",
                    BinarySyntaxKind.GreaterThanOrEqualTo => " >= ",
                    BinarySyntaxKind.LessThanOrEqualTo => " <= ",
                    BinarySyntaxKind.EqualTo => " == ",
                    BinarySyntaxKind.NotEqualTo => " != ",
                    _ => throw new Exception("This should never happen"),
                };
            }
            else if (syntax.Left.Tag.ReturnType == BoolType.Instance) {
                op = syntax.Kind switch {
                    BinarySyntaxKind.And => " && ",
                    BinarySyntaxKind.Or => " || ",
                    BinarySyntaxKind.Xor => " != ",
                    BinarySyntaxKind.EqualTo => " == ",
                    BinarySyntaxKind.NotEqualTo => " != ",
                    _ => throw new Exception("This should never happen"),
                };
            }
            else {
                throw new Exception("This should never happen");
            }                        

            var left = gen.Generate(syntax.Left, scope);
            var right = gen.Generate(syntax.Right, scope);

            return left.Combine(right, (x, y) => "(" + x + op + y + ")");
        }

        public CBlock GenerateAlloc(AllocSyntax<TypeCheckTag> syntax, ICScope scope, ICodeGenerator gen) {
            var target = gen.Generate(syntax.Target, scope);
            var innerType = gen.Generate(syntax.Target.Tag.ReturnType);
            var tempType = gen.Generate(syntax.Tag.ReturnType);
            var tempName = "$alloc_temp_" + this.allocTempCounter++;
            var writer = new CWriter();

            writer.Lines(target.SourceLines);
            writer.Line("// Allocation");
            writer.VariableInit(tempType, tempName, $"({tempType})malloc(sizeof({innerType}))");
            writer.VariableAssignment($"*({innerType}*){tempName}", $"{target.Value}");
            writer.Line($"{tempName} |= 1;");
            writer.EmptyLine();

            scope.SetVariableUndestructed(tempName, syntax.Tag.ReturnType);

            return writer.ToBlock(tempName);
        }
    }
}