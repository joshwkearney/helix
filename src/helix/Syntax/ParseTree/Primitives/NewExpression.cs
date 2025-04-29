using Helix.Parsing;
using Helix.Syntax.ParseTree.Structs;
using Helix.Syntax.ParseTree.Unions;
using Helix.Syntax.TypedTree;
using Helix.Syntax.TypedTree.Primitives;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Primitives;

public class NewExpression : IParseExpression {
    public required TokenLocation Location { get; init; }
        
    public required IParseExpression TypeExpression { get; init; }

    public IReadOnlyList<string> Names { get; init; } = [];

    public IReadOnlyList<IParseExpression> Values { get; init; } = [];
        
    public TypeCheckResult<ITypedExpression> CheckTypes(TypeFrame types) {
        // Make sure our type is actually a type
        if (!this.TypeExpression.AsType(types).TryGetValue(out var type)) {
            throw TypeException.ExpectedTypeExpression(this.TypeExpression.Location);              
        }

        // Make sure we are not supplying members to a primitive type
        if (!type.AsStruct(types).HasValue && !type.AsUnion(types).HasValue) {
            if (this.Names.Count > 0) {
                throw new TypeException(
                    this.Location,
                    "Member Not Defined",
                    $"The type '{type}' does not contain the member '{this.Names[0]}'");
            }
        }

        // Handle normal put syntax
        if (type == PrimitiveType.Void) {
            var result = new VoidLiteral {
                Location = this.Location
            };
                
            return result.CheckTypes(types);
        }
        else if (type == PrimitiveType.Word) {
            var result = new WordLiteral {
                Location = this.Location,
                Value = 0
            };
                
            return result.CheckTypes(types);
        }
        else if (type == PrimitiveType.Bool) {
            var result = new BoolLiteral {
                Location = this.Location,
                Value = false
            };
                
            return result.CheckTypes(types);
        }
        else if (type is SingularWordType singInt) {
            var result = new WordLiteral {
                Location = this.Location,
                Value = singInt.Value
            };
                
            return result.CheckTypes(types);
        }
        else if (type is SingularBoolType singBool) {
            var result = new BoolLiteral {
                Location = this.Location,
                Value = singBool.Value
            };
                
            return result.CheckTypes(types);
        }
        else if (type.AsStruct(types).TryGetValue(out var structSig)) {
            var result = new NewStructExpression {
                Location = this.Location,
                StructSignature = structSig,
                StructType = type,
                Names = this.Names,
                Values = this.Values
            };

            return result.CheckTypes(types);
        }
        else if (type.AsUnion(types).TryGetValue(out var unionSig)) {
            var result = new NewUnionExpression {
                Location = this.Location,
                UnionSignature = unionSig,
                UnionType = type,
                Names = this.Names,
                Values = this.Values
            };

            return result.CheckTypes(types);
        }

        throw new TypeException(
            this.Location,
            "Invalid Initialization",
            $"The type '{type}' does not have a default value and cannot be initialized.");
    }
}