using System.Collections.Generic;
using System.Linq;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Features.Containers.Arrays;
using Trophy.Features.Containers.Structs;
using Trophy.Features.Containers.Unions;
using Trophy.Features.Primitives;
using Trophy.Parsing;

namespace Trophy.Features.Containers {
    public class StructArgument<T> {
        public string MemberName { get; set; }

        public T MemberValue { get; set; }
    }

    public class NewSyntaxA : ISyntaxA {
        public readonly IReadOnlyList<StructArgument<ISyntaxA>> args;
        public readonly TrophyType targetType;

        public TokenLocation Location { get; }

        public NewSyntaxA(TokenLocation location, TrophyType targetType, IReadOnlyList<StructArgument<ISyntaxA>> args) {
            this.Location = location;
            this.targetType = targetType;
            this.args = args;
        }

        public ISyntaxB CheckNames(INameRecorder names) {
            // Resolve the target type
            var target = names.ResolveTypeNames(this.targetType, this.Location);

            // Check for primitive types
            if (this.targetType.IsBoolType || this.targetType.IsIntType || this.targetType.IsVoidType) {
                if (this.args.Any()) {
                    throw TypeCheckingErrors.NewObjectHasExtraneousFields(this.Location, this.targetType, this.args.Select(x => x.MemberName));
                }

                if (this.targetType.IsBoolType) {
                    return new BoolLiteralSyntax(this.Location, false).CheckNames(names);
                }
                else if (this.targetType.IsIntType) {
                    return new IntLiteralSyntax(this.Location, 0).CheckNames(names);
                }
                else {
                    return new VoidLiteralAB(this.Location).CheckNames(names);
                }
            }

            // Check for array types
            if (this.targetType.AsArrayType().TryGetValue(out var arrayType)) {
                if (this.args.Any()) {
                    throw TypeCheckingErrors.NewObjectHasExtraneousFields(this.Location, this.targetType, this.args.Select(x => x.MemberName));
                }

                var arrayLiteral = new NewFixedArraySyntaxA(this.Location, new FixedArrayType(arrayType.ElementType, 0, arrayType.IsReadOnly));
                var asSyntax = new AsSyntaxA(this.Location, arrayLiteral, arrayType);

                return asSyntax.CheckNames(names);
            }

            // Check for fixed array types
            if (this.targetType.AsFixedArrayType().TryGetValue(out var fixedArrayType)) {
                if (this.args.Any()) {
                    throw TypeCheckingErrors.NewObjectHasExtraneousFields(this.Location, this.targetType, this.args.Select(x => x.MemberName));
                }

                return new NewFixedArraySyntaxA(this.Location, fixedArrayType).CheckNames(names);
            }

            // Check for struct and union types
            if (this.targetType.AsNamedType().TryGetValue(out var path) && names.TryGetName(path, out var nameTarget)) {
                if (nameTarget == NameTarget.Struct) {
                    return new NewStructSyntaxA(this.Location, this.targetType, args).CheckNames(names);
                }
                else if (nameTarget == NameTarget.Union) {
                    return new NewUnionSyntaxA(this.Location, this.targetType, this.args).CheckNames(names);
                }
            }

            throw TypeCheckingErrors.UnexpectedType(this.Location, this.targetType);
        }
    }
}