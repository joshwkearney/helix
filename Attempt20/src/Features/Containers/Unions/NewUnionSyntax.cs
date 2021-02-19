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

namespace Attempt20.Features.Containers.Unions {
    public class NewUnionParsedSyntax : IParsedSyntax {
        public TokenLocation Location { get; set; }

        public IReadOnlyList<StructArgument<IParsedSyntax>> Arguments { get; set; }

        public TrophyType Target { get; set; }

        public IdentifierPath TargetPath { get; private set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            // Make sure the target is defined
            if (!this.Target.AsNamedType().TryGetValue(out var path)) {
                throw TypeCheckingErrors.ExpectedUnionType(this.Location, this.Target);
            }

            // Make sure the target's path is defined
            if (!names.TryGetName(path, out var target)) {
                throw TypeCheckingErrors.ExpectedUnionType(this.Location, this.Target);
            }

            // Make sure the path is to a struct
            if (target != NameTarget.Union) {
                throw TypeCheckingErrors.ExpectedUnionType(this.Location, this.Target);
            }

            // Save the struct path
            this.TargetPath = path;

            // Make sure that there is exactly one argument
            if (this.Arguments.Count > 1) {
                throw TypeCheckingErrors.NewObjectHasExtraneousFields(this.Location, this.Target, this.Arguments.Select(x => x.MemberName));
            }

            // Check argument names
            this.Arguments = this.Arguments
                .Select(x => new StructArgument<IParsedSyntax>() {
                    MemberName = x.MemberName,
                    MemberValue = x.MemberValue.CheckNames(names)
                })
                .ToArray();

            return this;
        }

        public ISyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            var unionType = new NamedType(this.TargetPath);
            var unionSig = types.TryGetUnion(this.TargetPath).GetValue();

            // If there are no arguments return an empty union
            if (!this.Arguments.Any()) {
                var voidLiteral = new VoidLiteralSyntax() { Location = this.Location };

                return new VoidToUnionAdapter(voidLiteral, unionSig, unionType, types);
            }

            var arg = this.Arguments.First();
            var memberOpt = unionSig.Members.Where(x => x.MemberName == arg.MemberName).FirstOrNone();

            // Make sure the argument is defined on this union
            if (!memberOpt.TryGetValue(out var member)) {
                throw TypeCheckingErrors.MemberUndefined(this.Location, this.Target, arg.MemberName);
            }

            var argVal = this.Arguments.First().MemberValue.CheckTypes(names, types);

            // Make sure the types can match
            if (!types.TryUnifyTo(argVal, member.MemberType).TryGetValue(out var newArgVal)) {
                throw TypeCheckingErrors.UnexpectedType(this.Location, member.MemberType, argVal.ReturnType);
            }

            return new NewUnionTypeCheckedSyntax() {
                Location = this.Location,
                Argument = new StructArgument<ISyntax>() {
                    MemberName = member.MemberName,
                    MemberValue = newArgVal
                },
                Lifetimes = newArgVal.Lifetimes,
                ReturnType = unionType,
                TargetPath = this.TargetPath,
                UnionTag = unionSig.Members.ToList().IndexOf(member)
            };
        }
    }

    public class NewUnionTypeCheckedSyntax : ISyntax {
        private static int tempCounter = 0;

        public TokenLocation Location { get; set; }

        public StructArgument<ISyntax> Argument { get; set; }

        public IdentifierPath TargetPath { get; set; }

        public TrophyType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }

        public int UnionTag { get; set; }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter) {
            var ctype = writer.ConvertType(this.ReturnType);
            var cname = "$new_union_" + tempCounter++;
            var argVal = this.Argument.MemberValue.GenerateCode(writer, statWriter);

            // Write union variable
            statWriter.WriteStatement(CStatement.VariableDeclaration(ctype, cname));

            // Write tag assignment
            statWriter.WriteStatement(CStatement.Assignment(
                CExpression.MemberAccess(CExpression.VariableLiteral(cname), "tag"),
                CExpression.IntLiteral(this.UnionTag)));

            // Write member assignment
            statWriter.WriteStatement(CStatement.Assignment(
                CExpression.MemberAccess(CExpression.MemberAccess(CExpression.VariableLiteral(cname), "data"), this.Argument.MemberName),
                argVal));

            statWriter.WriteStatement(CStatement.NewLine());

            return CExpression.VariableLiteral(cname);
        }
    }
}
