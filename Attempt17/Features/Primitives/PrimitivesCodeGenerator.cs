using System;
using Attempt17.CodeGeneration;
using Attempt17.TypeChecking;
using Attempt17.Types;

namespace Attempt17.Features.Primitives {
    public class PrimitivesCodeGenerator : IPrimitivesVisitor<CBlock, TypeCheckTag, CodeGenerationContext> {
        private int allocTempCounter = 0;

        public CBlock VisitAlloc(AllocSyntax<TypeCheckTag> syntax, ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor, CodeGenerationContext context) {
            var target = syntax.Target.Accept(visitor, context);
            var innerType = context.Generator.Generate(syntax.Target.Tag.ReturnType);
            var tempType = context.Generator.Generate(syntax.Tag.ReturnType);
            var tempName = "$alloc_temp_" + this.allocTempCounter++;
            var writer = new CWriter();

            writer.Lines(target.SourceLines);
            writer.Line("// Allocation");
            writer.VariableInit(tempType, tempName, $"({tempType})malloc(sizeof({innerType}))");
            writer.VariableAssignment($"*({innerType}*){tempName}", $"{target.Value}");
            writer.Line($"{tempName} |= 1;");
            writer.EmptyLine();

            context.Scope.SetVariableUndestructed(tempName, syntax.Tag.ReturnType);

            return writer.ToBlock(tempName);
        }

        public CBlock VisitAs(AsSyntax<TypeCheckTag> syntax, ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor, CodeGenerationContext context) {
            throw new InvalidOperationException();
        }

        public CBlock VisitBinary(BinarySyntax<TypeCheckTag> syntax, ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor, CodeGenerationContext context) {
            string op;

            if (syntax.Left.Tag.ReturnType == IntType.Instance) {
                op = syntax.Kind switch
                {
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
                op = syntax.Kind switch
                {
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

            var left = syntax.Left.Accept(visitor, context);
            var right = syntax.Right.Accept(visitor, context);

            return left.Combine(right, (x, y) => "(" + x + op + y + ")");
        }

        public CBlock VisitBoolLiteral(BoolLiteralSyntax<TypeCheckTag> syntax, ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor, CodeGenerationContext context) {
            return new CBlock(syntax.Value ? "1" : "0");
        }

        public CBlock VisitIntLiteral(IntLiteralSyntax<TypeCheckTag> syntax, ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor, CodeGenerationContext context) {
            return new CBlock(syntax.Value.ToString() + "LL");
        }

        public CBlock VisitVoidLiteral(VoidLiteralSyntax<TypeCheckTag> syntax, ISyntaxVisitor<CBlock, TypeCheckTag, CodeGenerationContext> visitor, CodeGenerationContext context) {
            return new CBlock("0");
        }
    }
}