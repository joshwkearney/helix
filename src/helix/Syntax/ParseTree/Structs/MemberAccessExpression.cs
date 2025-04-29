using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.Syntax.TypedTree.Structs;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Structs;

public record MemberAccessExpression : IParseExpression {
    public required IParseExpression Operand { get; init; }

    public required string MemberName { get; init; }

    public required TokenLocation Location { get; init; }
    
    public TypeCheckResult<ITypedExpression> CheckTypes(TypeFrame types) {
        (var operand, types) = this.Operand.CheckTypes(types);

        // Handle getting the count of an array
        if (operand.ReturnType is ArrayType array) {
            if (this.MemberName == "count") {
                var result = new TypedMemberAccessExpression {
                    Location = this.Location,
                    Operand = operand,
                    MemberName = "count",
                    ReturnType = PrimitiveType.Word
                };

                return new TypeCheckResult<ITypedExpression>(result, types);
            }
        }

        if (operand.ReturnType.AsStruct(types).TryGetValue(out var sig)) {
            // If this is a struct we can access the fields
            var fieldOpt = sig
                .Members
                .Where(x => x.Name == this.MemberName)
                .FirstOrNone();

            // Make sure this field is present
            if (fieldOpt.TryGetValue(out var field)) {
                var result = new TypedMemberAccessExpression {
                    Location = this.Location,
                    Operand = operand,
                    MemberName = this.MemberName,
                    ReturnType = field.Type
                };

                return new TypeCheckResult<ITypedExpression>(result, types);
            }               
        }

        throw TypeException.MemberUndefined(this.Location, operand.ReturnType, this.MemberName);
    }
}