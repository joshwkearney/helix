using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Trophy.Features.Containers.Unions {
    public class NewUnionSyntaxA : ISyntaxA {
        private readonly IReadOnlyList<StructArgument<ISyntaxA>> args;
        private readonly TrophyType targetType;

        public TokenLocation Location { get; }

        public NewUnionSyntaxA(TokenLocation location, TrophyType target, IReadOnlyList<StructArgument<ISyntaxA>> args) {
            this.Location = location;
            this.targetType = target;
            this.args = args;
        }

        public ISyntaxB CheckNames(INameRecorder names) {
            // Make sure the target is defined
            if (!this.targetType.AsNamedType().TryGetValue(out var path)) {
                throw TypeCheckingErrors.ExpectedUnionType(this.Location, this.targetType);
            }

            // Make sure the target's path is defined
            if (!names.TryGetName(path, out var target)) {
                throw TypeCheckingErrors.ExpectedUnionType(this.Location, this.targetType);
            }

            // Make sure the path is to a union
            if (target != NameTarget.Union) {
                throw TypeCheckingErrors.ExpectedUnionType(this.Location, this.targetType);
            }

            // Make sure that there is at most one argument
            if (this.args.Count > 1) {
                throw TypeCheckingErrors.NewObjectHasExtraneousFields(this.Location, this.targetType, this.args.Select(x => x.MemberName));
            }

            // Check argument names
            var arg = this.args
                .Select(x => new StructArgument<ISyntaxB>() {
                    MemberName = x.MemberName,
                    MemberValue = x.MemberValue.CheckNames(names)
                })
                .FirstOrNone();

            return new NewUnionSyntaxB(this.Location, this.targetType, arg, path);
        }
    }

    public class NewUnionSyntaxB : ISyntaxB {
        private readonly IOption<StructArgument<ISyntaxB>> args;
        private readonly TrophyType target;
        private readonly IdentifierPath path;

        public TokenLocation Location { get; }

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage {
            get => ImmutableDictionary.Create<IdentifierPath, VariableUsageKind>();
        }

        public NewUnionSyntaxB(
            TokenLocation location, 
            TrophyType target, 
            IOption<StructArgument<ISyntaxB>> args, 
            IdentifierPath path) {

            this.Location = location;
            this.target = target;
            this.args = args;
            this.path = path;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var unionType = new NamedType(this.path);
            var unionSig = types.TryGetUnion(this.path).GetValue();

            // If there are no arguments return an empty union
            if (!this.args.TryGetValue(out var arg)) {
                var mem = unionSig.Members.First();

                if (!mem.MemberType.HasDefaultValue(types)) {
                    throw TypeCheckingErrors.TypeWithoutDefaultValue(this.Location, unionType);
                }

                var voidLiteral = new VoidLiteralC();
                var arg2 = new StructArgument<ISyntaxC>() {
                    MemberName = mem.MemberName,
                    MemberValue = types.TryUnifyTo(voidLiteral, mem.MemberType).GetValue()
                };

                // TODO - MAGIC ZERO
                return new NewUnionSyntaxC(arg2, 0, unionType);
            }

            var memberOpt = unionSig.Members.Where(x => x.MemberName == arg.MemberName).FirstOrNone();

            // Make sure the argument is defined on this union
            if (!memberOpt.TryGetValue(out var member)) {
                throw TypeCheckingErrors.MemberUndefined(this.Location, this.target, arg.MemberName);
            }

            var argVal = arg.MemberValue.CheckTypes(types);

            // Make sure the types can match
            if (!types.TryUnifyTo(argVal, member.MemberType).TryGetValue(out var newArgVal)) {
                throw TypeCheckingErrors.UnexpectedType(this.Location, member.MemberType, argVal.ReturnType);
            }

            var tag = unionSig.Members.ToList().IndexOf(member);
            var argc = new StructArgument<ISyntaxC>() {
                MemberName = member.MemberName,
                MemberValue = newArgVal
            };

            return new NewUnionSyntaxC(argc, tag, unionType);
        }
    };

    public class NewUnionSyntaxC : ISyntaxC {
        private static int tempCounter = 0;

        private readonly StructArgument<ISyntaxC> arg;
        public readonly int tag;

        public TrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => arg.MemberValue.Lifetimes;

        public NewUnionSyntaxC(StructArgument<ISyntaxC> arg, int tag, TrophyType returnType) {
            this.arg = arg;
            this.tag = tag;
            this.ReturnType = returnType;
        }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter) {
            var ctype = writer.ConvertType(this.ReturnType);
            var cname = "$new_union_" + tempCounter++;
            var argVal = this.arg.MemberValue.GenerateCode(writer, statWriter);

            // Write union variable
            statWriter.WriteStatement(CStatement.VariableDeclaration(ctype, cname));

            // Write tag assignment
            statWriter.WriteStatement(CStatement.Assignment(
                CExpression.MemberAccess(CExpression.VariableLiteral(cname), "tag"),
                CExpression.IntLiteral(this.tag)));

            // Write member assignment
            statWriter.WriteStatement(CStatement.Assignment(
                CExpression.MemberAccess(CExpression.MemberAccess(CExpression.VariableLiteral(cname), "data"), this.arg.MemberName),
                argVal));

            statWriter.WriteStatement(CStatement.NewLine());

            return CExpression.VariableLiteral(cname);
        }
    }
}
