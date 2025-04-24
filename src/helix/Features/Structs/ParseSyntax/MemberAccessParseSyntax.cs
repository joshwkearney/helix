using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Structs;

public record MemberAccessParseSyntax : IParseSyntax {
    public required IParseSyntax Operand { get; init; }

    public required string MemberName { get; init; }

    public required TokenLocation Location { get; init; }
        
    public bool IsPure => this.Operand.IsPure;
        
    public virtual ISyntax CheckTypes(TypeFrame types) {
        var operand = this.Operand.CheckTypes(types).ToRValue(types);

        // Handle getting the count of an array
        if (operand.ReturnType is ArrayType array) {
            if (this.MemberName == "count") {
                var result = new MemberAccessSyntax {
                    Location = this.Location,
                    Operand = operand,
                    MemberName = "count",
                    ReturnType = PrimitiveType.Word
                };

                return result;
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
                var result = new MemberAccessSyntax {
                    Location = this.Location,
                    Operand = operand,
                    MemberName = this.MemberName,
                    ReturnType = field.Type
                };

                return result;
            }               
        }

        throw TypeException.MemberUndefined(this.Location, operand.ReturnType, this.MemberName);
    }
}