using System.Collections.Generic;
using System.Collections.Immutable;
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
        private readonly IReadOnlyList<StructArgument<ISyntaxA>> args;
        private readonly ISyntaxA targetType;

        public TokenLocation Location { get; }

        public NewSyntaxA(TokenLocation location, ISyntaxA targetType, IReadOnlyList<StructArgument<ISyntaxA>> args) {
            this.Location = location;
            this.targetType = targetType;
            this.args = args;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            // Make sure the target type is valid by checking the names
            var targetType = this.targetType.CheckNames(names);

            // Structs and unions can only be properly created if we catch them prematurely
            if (this.targetType.ResolveToType(names).TryGetValue(out var type)) {
                if (type.AsNamedType().TryGetValue(out var path)) {
                    if (names.TryGetName(path, out var target)) {
                        if (target == NameTarget.Struct) {
                            return new NewStructSyntaxA(this.Location, type, this.args).CheckNames(names);
                        }
                        else if (target == NameTarget.Union) {
                            return new NewUnionSyntaxA(this.Location, type, this.args).CheckNames(names);
                        }
                    }
                }
            }

            var args = this.args
                .Select(x => new StructArgument<ISyntaxB>() {
                    MemberName = x.MemberName,
                    MemberValue = x.MemberValue.CheckNames(names)})
                .ToArray();

            return new NewSyntaxB(this.Location, targetType, names.Context.Region, args);
        }
    }

    public class NewSyntaxB : ISyntaxB {
        private readonly IReadOnlyList<StructArgument<ISyntaxB>> args;
        private readonly ISyntaxB targetType;
        private readonly IdentifierPath region;

        public TokenLocation Location { get; }

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage {
            get {
                return args.Select(x => x.MemberValue.VariableUsage)
                    .Aggregate(this.targetType.VariableUsage, (x, y) => x.AddRange(y));
            }
        }

        public NewSyntaxB(TokenLocation location, ISyntaxB targetType, IdentifierPath region, IReadOnlyList<StructArgument<ISyntaxB>> args) {
            this.Location = location;
            this.targetType = targetType;
            this.args = args;
            this.region = region;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            // Resolve the target type
            var target = this.targetType.CheckTypes(types);

            if (!target.ReturnType.AsMetaType().Select(x => x.PayloadType).TryGetValue(out var returnType)) {
                throw TypeCheckingErrors.ExpectedTypeExpression(this.targetType.Location);
            }

            // Check for primitive types
            if (returnType.IsBoolType || returnType.IsIntType || returnType.IsVoidType) {
                if (this.args.Any()) {
                    throw TypeCheckingErrors.NewObjectHasExtraneousFields(this.Location, returnType, this.args.Select(x => x.MemberName));
                }

                if (returnType.IsBoolType) {
                    return new BoolLiteralSyntax(this.Location, false).CheckTypes(types);
                }
                else if (returnType.IsIntType) {
                    return new IntLiteralSyntax(this.Location, 0).CheckTypes(types);
                }
                else {
                    return new VoidLiteralAB(this.Location).CheckTypes(types);
                }
            }

            // Check for fixed array types
            if (returnType.AsFixedArrayType().TryGetValue(out var fixedArray)) {
                if (this.args.Any()) {
                    throw TypeCheckingErrors.NewObjectHasExtraneousFields(this.Location, returnType, this.args.Select(x => x.MemberName));
                }

                return new NewFixedArraySyntaxBC(this.Location, fixedArray, this.region);
            }

            // Check for array types
            if (returnType.AsArrayType().Any()) {
                if (this.args.Any()) {
                    throw TypeCheckingErrors.NewObjectHasExtraneousFields(this.Location, returnType, this.args.Select(x => x.MemberName));
                }

                return new VoidToArrayAdapterC(new VoidLiteralC(), returnType);
            }

            // Structs and unions have already been checked
            throw TypeCheckingErrors.UnexpectedType(this.Location, returnType);
        }
    }
}