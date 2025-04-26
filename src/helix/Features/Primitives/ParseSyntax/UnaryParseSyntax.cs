using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.Primitives.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Primitives.ParseSyntax {
    public record UnaryParseSyntax : IParseSyntax {
        public required TokenLocation Location { get; init; }
        
        public required UnaryOperatorKind Operator { get; init; }
        
        public required IParseSyntax Operand { get; init; }
        
        public bool IsPure => this.Operand.IsPure;

        public TypeCheckResult CheckTypes(TypeFrame types) {
            if (this.Operator == UnaryOperatorKind.Plus || this.Operator == UnaryOperatorKind.Minus) {
                var left = new WordLiteral {
                    Location = this.Location,
                    Value = 0
                };

                var op = this.Operator == UnaryOperatorKind.Plus
                    ? BinaryOperationKind.Add
                    : BinaryOperationKind.Subtract;

                var result = new BinaryParseSyntax {
                    Location = this.Location,
                    Left = left,
                    Right = this.Operand,
                    Operator = op
                };

                return result.CheckTypes(types);
            }
            else if (this.Operator == UnaryOperatorKind.Not) {
                (var arg, types) = this.Operand.CheckTypes(types);
                var returnType = arg.ReturnType;

                if (returnType is SingularBoolType singularBool) {
                    returnType = new SingularBoolType(!singularBool.Value);
                }
                else {
                    arg = arg.UnifyTo(PrimitiveType.Bool, types);
                    returnType = PrimitiveType.Bool;
                }

                var result = new UnaryNotSyntax {
                    Location = this.Location,
                    Operand = arg,
                    ReturnType = returnType,
                    AlwaysJumps = arg.AlwaysJumps
                };

                return new TypeCheckResult(result, types);
            }
            else {
                throw new Exception("Unexpected unary operator kind");
            }
        }
    }

}