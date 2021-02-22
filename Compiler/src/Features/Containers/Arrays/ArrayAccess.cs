using System.Collections.Immutable;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Features.Variables;
using Attempt20.Parsing;

namespace Attempt20.Features.Containers.Arrays {
    public enum ArrayAccessKind {
        ValueAccess, LiteralAccess
    }

    public class ArrayAccessSyntaxA : ISyntaxA {
        private readonly ISyntaxA target, index;
        private readonly ArrayAccessKind accessKind;
        public TokenLocation Location { get; }

        public ArrayAccessSyntaxA(TokenLocation location, ISyntaxA target, ISyntaxA index, ArrayAccessKind accessKind) {
            this.Location = location;
            this.target = target;
            this.index = index;
            this.accessKind = accessKind;
        }

        public ISyntaxB CheckNames(INameRecorder names) {
            var target = this.target.CheckNames(names);
            var index = this.index.CheckNames(names);
            var result = new ArrayLiteralAccessSyntaxB(this.Location, target, index);

            if (this.accessKind == ArrayAccessKind.ValueAccess) {
                return new DereferenceSyntaxB(result);
            }
            else {
                return result;
            }
        }
    }

    public class ArrayLiteralAccessSyntaxB : ISyntaxB {
        private readonly ISyntaxB target, index;

        public TokenLocation Location { get; }

        public ArrayLiteralAccessSyntaxB(TokenLocation location, ISyntaxB target, ISyntaxB index) {
            this.Location = location;
            this.target = target;
            this.index = index;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var target = this.target.CheckTypes(types);
            var index = this.index.CheckTypes(types);
            var elementType = TrophyType.Void;

            // Make sure the target is an array
            if (target.ReturnType.AsArrayType().TryGetValue(out var arrayType)) {
                elementType = arrayType.ElementType;
            }
            else if (target.ReturnType.AsFixedArrayType().TryGetValue(out var fixedArrayType)) {
                elementType = fixedArrayType.ElementType;
            }
            else {
                throw TypeCheckingErrors.ExpectedArrayType(this.target.Location, target.ReturnType);
            }

            // Make sure the index in an int
            if (types.TryUnifyTo(index, TrophyType.Integer).TryGetValue(out var newIndex)) {
                index = newIndex;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(this.index.Location, TrophyType.Integer, index.ReturnType);
            }

            return new ArrayLiteralAccessSyntaxC(target, index, new VariableType(elementType));
        }
    }

    public class ArrayLiteralAccessSyntaxC : ISyntaxC {
        private readonly ISyntaxC target, index;

        public TrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.target.Lifetimes;

        public ArrayLiteralAccessSyntaxC(ISyntaxC target, ISyntaxC index, TrophyType returnType) {
            this.target = target;
            this.index = index;
            this.ReturnType = returnType;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var index = this.index.GenerateCode(declWriter, statWriter);
            var target = this.target.GenerateCode(declWriter, statWriter);
            var targetSize = CExpression.MemberAccess(target, "size");
            var targetData = CExpression.MemberAccess(target, "data");

            var cond = CExpression.BinaryExpression(
                CExpression.BinaryExpression(index, CExpression.IntLiteral(0), Primitives.BinaryOperation.LessThan),
                CExpression.BinaryExpression(index, targetSize, Primitives.BinaryOperation.GreaterThanOrEqualTo),
                Primitives.BinaryOperation.Or);

            var ifStat = CStatement.If(cond, new[] {
                CStatement.FromExpression(CExpression.Invoke(CExpression.VariableLiteral("fprintf"), new[] {
                    CExpression.VariableLiteral("stderr"),
                    CExpression.StringLiteral("array_out_of_bounds") })),
                CStatement.FromExpression(CExpression.Invoke(CExpression.VariableLiteral("exit"), new[]{
                    CExpression.IntLiteral(-1) }))
            });

            statWriter.WriteStatement(ifStat);
            statWriter.WriteStatement(CStatement.NewLine());

            return CExpression.BinaryExpression(targetData, index, Primitives.BinaryOperation.Add);
        }
    }
}
