using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation.CSyntax;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Trophy.Features.Containers.Structs {
    public class NewStructSyntaxA : ISyntaxA {
        private readonly IReadOnlyList<StructArgument<ISyntaxA>> args;
        private readonly ITrophyType targetType;

        public TokenLocation Location { get; }

        public NewStructSyntaxA(TokenLocation location, ITrophyType targetType, IReadOnlyList<StructArgument<ISyntaxA>> args) {
            this.Location = location;
            this.targetType = targetType;
            this.args = args;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            // Make sure the target is defined
            if (!this.targetType.AsNamedType().TryGetValue(out var path)) {
                throw TypeCheckingErrors.ExpectedStructType(this.Location, this.targetType);
            }

            // Make sure the target's path is defined
            if (!names.TryGetName(path, out var target)) {
                throw TypeCheckingErrors.ExpectedStructType(this.Location, this.targetType);
            }

            // Make sure the path is to a struct
            if (target != NameTarget.Struct) {
                throw TypeCheckingErrors.ExpectedStructType(this.Location, this.targetType);
            }

            // Check argument names
            var args = this.args
                .Select(x => new StructArgument<ISyntaxB>() {
                    MemberName = x.MemberName,
                    MemberValue = x.MemberValue.CheckNames(names)
                })
                .ToArray();

            var dups = this.args
                .Select(x => x.MemberName)
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();

            // Make sure there are no duplicated names
            if (dups.Any()) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, dups.First());
            }

            return new NewStructSyntaxB(this.Location, path, args);
        }
    }

    public class NewStructSyntaxB : ISyntaxB {
        private readonly IReadOnlyList<StructArgument<ISyntaxB>> args;
        private readonly IdentifierPath targetPath;

        public TokenLocation Location { get; }

        public IImmutableSet<VariableUsage> VariableUsage {
            get => this.args
                .SelectMany(x => x.MemberValue.VariableUsage)
                .ToImmutableHashSet();
        }

        public NewStructSyntaxB(
            TokenLocation location, 
            IdentifierPath targetPath, 
            IReadOnlyList<StructArgument<ISyntaxB>> args) {

            this.Location = location;
            this.targetPath = targetPath;
            this.args = args;
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            var structType = new NamedType(this.targetPath);
            var structSig = types.TryGetStruct(this.targetPath).GetValue();

            var undefinedFields = this.args
                .Select(x => x.MemberName)
                .Except(structSig.Members.Select(x => x.MemberName))
                .ToArray();

            // Make sure that all members are defined in the struct
            if (undefinedFields.Any()) {
                throw TypeCheckingErrors.NewObjectHasExtraneousFields(this.Location, structType, undefinedFields);
            }

            var absentFields = structSig.Members
                .Select(x => x.MemberName)
                .Except(this.args.Select(x => x.MemberName))
                .Select(x => structSig.Members.First(y => x == y.MemberName))
                .ToArray();

            var requiredAbsentFields = absentFields
                .Where(x => !x.MemberType.HasDefaultValue(types))
                .ToArray();

            // Make sure that all the missing members have a default value
            if (requiredAbsentFields.Any()) {
                throw TypeCheckingErrors.NewObjectMissingFields(
                    this.Location, 
                    structType, 
                    requiredAbsentFields.Select(x => x.MemberName));
            }

            var voidLiteral = new VoidLiteralC();

            // Generate syntax for the missing fields
            var restoredAbsentFields = absentFields
                .Select(x => new StructArgument<ISyntaxC>() {
                    MemberName = x.MemberName,
                    MemberValue = types.TryUnifyTo(voidLiteral, x.MemberType).GetValue()
                })
                .ToArray();

            // Type check and merge the fields together
            var allFields = this.args
                .Select(x => new StructArgument<ISyntaxC>() {
                    MemberName = x.MemberName,
                    MemberValue = x.MemberValue.CheckTypes(types)
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
                    throw TypeCheckingErrors.UnexpectedType(this.Location, field.MemberValue.ReturnType);
                }
            }

            return new NewStructSyntaxC(allFields, structType);
        }
    }

    public class NewStructSyntaxC : ISyntaxC {
        private static int tempCounter = 0;

        private readonly IReadOnlyList<StructArgument<ISyntaxC>> args;

        public ITrophyType ReturnType { get; }

        public NewStructSyntaxC(
            IReadOnlyList<StructArgument<ISyntaxC>> args, 
            ITrophyType returnType) {

            this.args = args;
            this.ReturnType = returnType;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var ctype = declWriter.ConvertType(this.ReturnType);
            var cname = "new_struct_" + tempCounter++;
            var mems = this.args.Select(x => new StructArgument<CExpression>() {
                MemberName = x.MemberName,
                MemberValue = x.MemberValue.GenerateCode(declWriter, statWriter)
            })
            .ToArray();

            statWriter.WriteStatement(CStatement.Comment($"New struct literal for '{this.ReturnType}'"));
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