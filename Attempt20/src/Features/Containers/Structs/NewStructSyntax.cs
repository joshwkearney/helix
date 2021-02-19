using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Features.Containers;
using Attempt20.Features.Primitives;
using Attempt20.Parsing;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt20.Features.Containers.Structs {
    public class NewStructParsedSyntax : IParsedSyntax {
        public TokenLocation Location { get; set; }

        public IReadOnlyList<StructArgument<IParsedSyntax>> Arguments { get; set; }

        public TrophyType Target { get; set; }

        public IdentifierPath TargetPath { get; private set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            // Make sure the target is defined
            if (!this.Target.AsNamedType().TryGetValue(out var path)) {
                throw TypeCheckingErrors.ExpectedStructType(this.Location, this.Target);
            }

            // Make sure the target's path is defined
            if (!names.TryGetName(path, out var target)) {
                throw TypeCheckingErrors.ExpectedStructType(this.Location, this.Target);
            }

            // Make sure the path is to a struct
            if (target != NameTarget.Struct) {
                throw TypeCheckingErrors.ExpectedStructType(this.Location, this.Target);
            }

            // Save the struct path
            this.TargetPath = path;

            // Check argument names
            this.Arguments = this.Arguments
                .Select(x => new StructArgument<IParsedSyntax>() {
                    MemberName = x.MemberName,
                    MemberValue = x.MemberValue.CheckNames(names)
                })
                .ToArray();

            var dups = this.Arguments
                .Select(x => x.MemberName)
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();

            // Make sure there are no duplicated names
            if (dups.Any()) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, dups.First());
            }

            return this;
        }

        public ISyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            var structType = new NamedType(this.TargetPath);
            var structSig = types.TryGetStruct(this.TargetPath).GetValue();

            var undefinedFields = this.Arguments
                .Select(x => x.MemberName)
                .Except(structSig.Members.Select(x => x.MemberName))
                .ToArray();

            // Make sure that all members are defined in the struct
            if (undefinedFields.Any()) {
                throw TypeCheckingErrors.NewObjectHasExtraneousFields(this.Location, structType, undefinedFields);
            }

            var absentFields = structSig.Members
                .Select(x => x.MemberName)
                .Except(this.Arguments.Select(x => x.MemberName))
                .Select(x => structSig.Members.First(y => x == y.MemberName))
                .ToArray();

            var requiredAbsentFields = absentFields
                .Where(x => !x.MemberType.HasDefaultValue(types))
                .ToArray();

            // Make sure that all the missing members have a default value
            if (requiredAbsentFields.Any()) {
                throw TypeCheckingErrors.NewObjectMissingFields(this.Location, structType, requiredAbsentFields.Select(x => x.MemberName));
            }

            var voidLiteral = new VoidLiteralSyntax() { Location = this.Location };

            // Generate syntax for the missing fields
            var restoredAbsentFields = absentFields
                .Select(x => new StructArgument<ISyntax>() {
                    MemberName = x.MemberName,
                    MemberValue = types.TryUnifyTo(voidLiteral, x.MemberType).GetValue()
                })
                .ToArray();

            // Type check and merge the fields together
            var allFields = this.Arguments
                .Select(x => new StructArgument<ISyntax>() {
                    MemberName = x.MemberName,
                    MemberValue = x.MemberValue.CheckTypes(names, types)
                })
                .Concat(restoredAbsentFields)
                .ToArray();

            // Make sure the field types match the signature
            foreach (var field in allFields) {
                var type = structSig.Members.First(x => x.MemberName == field.MemberName).MemberType;

                if (types.TryUnifyTo(field.MemberValue, type).TryGetValue(out var newValue)) {
                    field.MemberValue = newValue;
                }
                else {
                    throw TypeCheckingErrors.UnexpectedType(field.MemberValue.Location, field.MemberValue.ReturnType);
                }
            }

            return new NewStructTypeCheckedSyntax() {
                Location = this.Location,
                Arguments = allFields,
                Lifetimes = allFields.Aggregate(ImmutableHashSet.Create<IdentifierPath>(), (x, y) => x.Union(y.MemberValue.Lifetimes)),
                ReturnType = structType,
                TargetPath = this.TargetPath
            };
        }
    }

    public class NewStructTypeCheckedSyntax : ISyntax {
        private static int tempCounter = 0;

        public TokenLocation Location { get; set; }

        public IReadOnlyList<StructArgument<ISyntax>> Arguments { get; set; }

        public IdentifierPath TargetPath { get; set; }

        public TrophyType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var ctype = declWriter.ConvertType(this.ReturnType);
            var cname = "$new_struct_" + tempCounter++;
            var mems = this.Arguments.Select(x => new StructArgument<CExpression>() {
                MemberName = x.MemberName,
                MemberValue = x.MemberValue.GenerateCode(declWriter, statWriter)
            })
            .ToArray();

            statWriter.WriteStatement(CStatement.VariableDeclaration(ctype, cname));

            // Write members
            foreach (var mem in mems) {
                statWriter.WriteStatement(CStatement.Assignment(
                    CExpression.MemberAccess(CExpression.VariableLiteral(cname), mem.MemberName),
                    mem.MemberValue));
            }

            statWriter.WriteStatement(CStatement.NewLine());

            return CExpression.VariableLiteral(cname);
        }
    }
}