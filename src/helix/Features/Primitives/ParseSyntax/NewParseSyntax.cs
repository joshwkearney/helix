using Helix.Analysis.Types;
using Helix.Parsing;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis;
using Helix.Features.Structs;
using Helix.Features.Unions;

namespace Helix.Features.Primitives {
    public class NewParseSyntax : IParseSyntax {
        public required TokenLocation Location { get; init; }
        
        public required IParseSyntax TypeSyntax { get; init; }

        public IReadOnlyList<string> Names { get; init; } = [];

        public IReadOnlyList<IParseSyntax> Values { get; init; } = [];

        public bool IsPure => this.TypeSyntax.IsPure && this.Values.All(x => x.IsPure);

        public TypeCheckResult CheckTypes(TypeFrame types) {
            // Make sure our type is actually a type
            if (!this.TypeSyntax.AsType(types).TryGetValue(out var type)) {
                throw TypeException.ExpectedTypeExpression(this.TypeSyntax.Location);              
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
                var result = new NewStructParseSyntax {
                    Location = this.Location,
                    Signature = structSig,
                    Names = this.Names,
                    Values = this.Values
                };

                return result.CheckTypes(types);
            }
            else if (type.AsUnion(types).TryGetValue(out var unionSig)) {
                var result = new NewUnionParseSyntax {
                    Location = this.Location,
                    Signature = unionSig,
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
}