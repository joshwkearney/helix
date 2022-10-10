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

    public class CreateTypeSyntaxA : ISyntaxA {
        private readonly IReadOnlyList<StructArgument<ISyntaxA>> args;
        private readonly ISyntaxA targetType;
        private readonly bool isStackAllocated;

        public TokenLocation Location { get; }

        public CreateTypeSyntaxA(TokenLocation location, ISyntaxA targetType, IReadOnlyList<StructArgument<ISyntaxA>> args, bool isStackAllocated) {
            this.Location = location;
            this.targetType = targetType;
            this.args = args;
            this.isStackAllocated = isStackAllocated;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            // Make sure the target type is valid by checking the names
            var targetType = this.targetType.CheckNames(names);

            // Structs, unions, and arrays can only be properly created if we catch them prematurely
            if (this.targetType.ResolveToType(names).TryGetValue(out var type)) {
                if (this.isStackAllocated) {
                    if (type.AsNamedType().TryGetValue(out var path) && names.TryGetName(path, out var target)) {
                        if (target == NameTarget.Struct) {
                            return new NewStructSyntaxA(this.Location, type, this.args).CheckNames(names);
                        }
                        else if (target == NameTarget.Union) {
                            return new NewUnionSyntaxA(this.Location, type, this.args).CheckNames(names);
                        }                        
                    }
                }
                else {
                    if (type.AsArrayType().TryGetValue(out var arrayType)) {
                        return new NewArraySyntaxA(this.Location, arrayType, this.args).CheckNames(names);
                    }
                }
            }

            var args = this.args
                .Select(x => new StructArgument<ISyntaxB>() {
                    MemberName = x.MemberName,
                    MemberValue = x.MemberValue.CheckNames(names)})
                .ToArray();

            return new CreateTypeSyntaxB(this.Location, targetType, this.isStackAllocated, args);
        }
    }

    public class CreateTypeSyntaxB : ISyntaxB {
        private readonly IReadOnlyList<StructArgument<ISyntaxB>> args;
        private readonly ISyntaxB targetType;
        private readonly bool isStackAllocated;

        public TokenLocation Location { get; }

        public IImmutableSet<VariableUsage> VariableUsage {
            get => args
                .Select(x => x.MemberValue.VariableUsage)
                .Aggregate(this.targetType.VariableUsage, (x, y) => x.Union(y));
        }

        public CreateTypeSyntaxB(TokenLocation location, ISyntaxB targetType, bool isStackAllocated, IReadOnlyList<StructArgument<ISyntaxB>> args) {
            this.Location = location;
            this.targetType = targetType;
            this.args = args;
            this.isStackAllocated = isStackAllocated;
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            // Resolve the target type
            var target = this.targetType.CheckTypes(types);

            if (!target.ReturnType.AsMetaType().Select(x => x.PayloadType).TryGetValue(out var returnType)) {
                throw TypeCheckingErrors.ExpectedTypeExpression(this.targetType.Location);
            }

            // All remaining types are stack allocated
            if (!this.isStackAllocated) {
                throw TypeCheckingErrors.InvalidHeapAllocation(this.Location, returnType);
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

            // All types have been checked at this point
            if (this.isStackAllocated) {
                throw TypeCheckingErrors.InvalidStackAllocation(this.Location, returnType);
            }
            else {
                throw TypeCheckingErrors.InvalidHeapAllocation(this.Location, returnType);
            }
        }
    }
}