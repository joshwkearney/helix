using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Features.Containers.Arrays;
using Attempt20.Features.Primitives;
using Attempt20.Parsing;
using Attempt20.src.Features.Containers.Structs;
using Attempt20.src.Features.Containers.Unions;

namespace Attempt20.Features.Containers {
    public class StructArgument<T> {
        public string MemberName { get; set; }

        public T MemberValue { get; set; }
    }

    public class NewParsedSyntax : IParsedSyntax {
        public TokenLocation Location { get; set; }

        public IReadOnlyList<StructArgument<IParsedSyntax>> Arguments { get; set; }

        public TrophyType Target { get; set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            // Resolve the target type
            this.Target = names.ResolveTypeNames(this.Target, this.Location);

            // Check for primitive types
            if (this.Target.IsBoolType || this.Target.IsIntType || this.Target.IsVoidType) {
                if (this.Arguments.Any()) {
                    throw TypeCheckingErrors.NewObjectHasExtraneousFields(this.Location, this.Target, this.Arguments.Select(x => x.MemberName));
                }

                if (this.Target.IsBoolType) {
                    return new BoolLiteralSyntax() { Location = this.Location, Value = false }.CheckNames(names);
                }
                else if (this.Target.IsIntType) {
                    return new IntLiteralSyntax() { Location = this.Location, Value = 0 }.CheckNames(names);
                }
                else {
                    return new VoidLiteralSyntax() { Location = this.Location }.CheckNames(names);
                }
            }

            // Check for array types
            if (this.Target.AsArrayType().TryGetValue(out var arrayType)) {
                if (this.Arguments.Any()) {
                    throw TypeCheckingErrors.NewObjectHasExtraneousFields(this.Location, this.Target, this.Arguments.Select(x => x.MemberName));
                }

                return new ArrayParsedLiteral() { Location = this.Location, Arguments = new IParsedSyntax[0] }.CheckNames(names);
            }

            // Check for fixed array types
            if (this.Target.AsFixedArrayType().TryGetValue(out var fixedArrayType)) {
                if (this.Arguments.Any()) {
                    throw TypeCheckingErrors.NewObjectHasExtraneousFields(this.Location, this.Target, this.Arguments.Select(x => x.MemberName));
                }

                return new NewFixedArraySyntax() { ArrayType = fixedArrayType, Location = this.Location }.CheckNames(names);
            }

            // Check for struct and union types
            if (this.Target.AsNamedType().TryGetValue(out var path) && names.TryGetName(path, out var target)) {
                if (target == NameTarget.Struct) {
                    return new NewStructParsedSyntax() {
                        Arguments = this.Arguments,
                        Location = this.Location,
                        Target = this.Target
                    }
                    .CheckNames(names);
                }
                else if (target == NameTarget.Union) {
                    return new NewUnionParsedSyntax() {
                        Arguments = this.Arguments,
                        Location = this.Location,
                        Target = this.Target
                    }
                   .CheckNames(names);
                }
            }

            throw TypeCheckingErrors.UnexpectedType(this.Location, this.Target);
        }

        public ISyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            throw new Exception("Internal compiler inconsistency");
        }
    }
}