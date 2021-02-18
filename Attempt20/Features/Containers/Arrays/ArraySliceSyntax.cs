using System;
using System.Collections.Immutable;
using Attempt20.CodeGeneration;
using Attempt20.Features.Primitives;

namespace Attempt20.Features.Arrays {
    public class ArraySliceParsedSyntax : IParsedSyntax {
        public TokenLocation Location { get; set; }

        public IParsedSyntax Target { get; set; }

        public IOption<IParsedSyntax> StartIndex { get; set; }

        public IOption<IParsedSyntax> EndIndex { get; set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            this.Target = this.Target.CheckNames(names);
            this.StartIndex = this.StartIndex.Select(x => x.CheckNames(names));
            this.EndIndex = this.EndIndex.Select(x => x.CheckNames(names));

            return this;
        }

        public ITypeCheckedSyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            var target = this.Target.CheckTypes(names, types);
            var startIndex = this.StartIndex.Select(x => x.CheckTypes(names, types));
            var endIndex = this.EndIndex.Select(x => x.CheckTypes(names, types));
            var returnType = LanguageType.Void;

            // Make sure the target is an array
            if (target.ReturnType.AsArrayType().TryGetValue(out var arrayType)) {
                returnType = arrayType;
            }
            else if (target.ReturnType.AsFixedArrayType().TryGetValue(out var fixedArrayType)) {
                returnType = new ArrayType(fixedArrayType.ElementType);
            }
            else {
                throw TypeCheckingErrors.ExpectedArrayType(target.Location, target.ReturnType);
            }

            // Make sure the index in an int
            startIndex = startIndex.Select(x => {
                if (types.TryUnifyTo(x, LanguageType.Integer).TryGetValue(out var newStart)) {
                    return newStart;
                }
                else {
                    throw TypeCheckingErrors.UnexpectedType(x.Location, LanguageType.Integer, x.ReturnType);
                }
            });

            // Make sure the length in an int
            endIndex = endIndex.Select(x => {
                if (types.TryUnifyTo(x, LanguageType.Integer).TryGetValue(out var newEnd)) {
                    return newEnd;
                }
                else {
                    throw TypeCheckingErrors.UnexpectedType(x.Location, LanguageType.Integer, x.ReturnType);
                }
            });

            return new ArraySliceTypeCheckedSyntax() {
                Location = this.Location,
                ReturnType = returnType,
                Lifetimes = target.Lifetimes,
                Target = target,
                StartIndex = startIndex,
                EndIndex = endIndex
            };
        }
    }

    public class ArraySliceTypeCheckedSyntax : ITypeCheckedSyntax {
        private static int sliceCounter = 0;

        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }

        public ITypeCheckedSyntax Target { get; set; }

        public IOption<ITypeCheckedSyntax> StartIndex { get; set; }

        public IOption<ITypeCheckedSyntax> EndIndex { get; set; }

        public CExpression GenerateCode(ICDeclarationWriter declWriter, ICStatementWriter statWriter) {
            var target = this.Target.GenerateCode(declWriter, statWriter);
            var sizeExpr = CExpression.MemberAccess(target, "size");

            var start = this.StartIndex
                .Select(x => x.GenerateCode(declWriter, statWriter))
                .GetValueOr(() => CExpression.IntLiteral(0));

            var end = this.EndIndex
                .Select(x => x.GenerateCode(declWriter, statWriter))
                .GetValueOr(() => sizeExpr);

            var cond = CExpression.BinaryExpression(
                CExpression.BinaryExpression(start, CExpression.IntLiteral(0), BinaryOperation.LessThan),
                CExpression.BinaryExpression(
                    CExpression.BinaryExpression(end, start, BinaryOperation.LessThanOrEqualTo),
                    CExpression.BinaryExpression(end, sizeExpr, BinaryOperation.GreaterThan),
                    BinaryOperation.Or),
                BinaryOperation.Or);

            var ifStat = CStatement.If(cond, new[] {
                CStatement.FromExpression(CExpression.Invoke(CExpression.VariableLiteral("fprintf"), new[] {
                    CExpression.VariableLiteral("stderr"),
                    CExpression.StringLiteral("array_slice_out_of_bounds at " + this.Location.StartIndex + ":" + this.Location.Length) })),
                CStatement.FromExpression(CExpression.Invoke(CExpression.VariableLiteral("exit"), new[]{
                    CExpression.IntLiteral(-1) }))
            });

            statWriter.WriteStatement(ifStat);
            statWriter.WriteStatement(CStatement.NewLine());

            var arrayName = "$array_slice_" + sliceCounter++;
            var arrayType = declWriter.ConvertType(this.ReturnType);

            statWriter.WriteStatement(CStatement.VariableDeclaration(arrayType, arrayName));
            statWriter.WriteStatement(CStatement.Assignment(
                CExpression.MemberAccess(CExpression.VariableLiteral(arrayName), "data"),
                CExpression.BinaryExpression(
                    CExpression.MemberAccess(target, "data"),
                    start,
                    BinaryOperation.Add)));
            statWriter.WriteStatement(CStatement.Assignment(
                CExpression.MemberAccess(CExpression.VariableLiteral(arrayName), "size"),
                CExpression.BinaryExpression(end, start, BinaryOperation.Subtract)));
            statWriter.WriteStatement(CStatement.NewLine());

            return CExpression.VariableLiteral(arrayName);
        }
    }
}
