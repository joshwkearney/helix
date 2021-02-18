using System;
using System.Collections.Immutable;
using Attempt20.CodeGeneration;

namespace Attempt20.Features.Arrays {
    public enum ArrayAccessKind {
        ValueAccess, LiteralAccess
    }

    public class ArrayAccessParsedSyntax : IParsedSyntax {
        public TokenLocation Location { get; set; }

        public IParsedSyntax Target { get; set; }

        public IParsedSyntax Index { get; set; }

        public ArrayAccessKind AccessKind { get; set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            this.Target = this.Target.CheckNames(names);
            this.Index = this.Index.CheckNames(names);

            return this;
        }

        public ITypeCheckedSyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            var target = this.Target.CheckTypes(names, types);
            var index = this.Index.CheckTypes(names, types);
            var lifetimes = ImmutableHashSet.Create<IdentifierPath>();
            var elementType = LanguageType.Void;

            // Make sure the target is an array
            if (target.ReturnType.AsArrayType().TryGetValue(out var arrayType)) {
                elementType = arrayType.ElementType;
            }
            else if (target.ReturnType.AsFixedArrayType().TryGetValue(out var fixedArrayType)) {
                elementType = fixedArrayType.ElementType;
            }
            else {
                throw TypeCheckingErrors.ExpectedArrayType(target.Location, target.ReturnType);
            }

            // Make sure the index in an int
            if (types.TryUnifyTo(index, LanguageType.Integer).TryGetValue(out var newIndex)) {
                index = newIndex;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(index.Location, LanguageType.Integer, index.ReturnType);
            }

            // We only need lifetimes if the array type is conditionally copiable
            if (this.AccessKind == ArrayAccessKind.LiteralAccess || elementType.GetCopiability(types) == TypeCopiability.Conditional) {
                lifetimes = target.Lifetimes;
            }

            var returnType = elementType;
            if (this.AccessKind == ArrayAccessKind.LiteralAccess) {
                returnType = new VariableType(returnType);
            }

            return new ArrayIndexTypeCheckedSyntax() {
                Location = this.Location,
                Index = index,
                Target = target,
                Lifetimes = lifetimes,
                ReturnType = returnType,
                AccessKind = this.AccessKind
            };
        }
    }

    public class ArrayIndexTypeCheckedSyntax : ITypeCheckedSyntax {
        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }

        public ITypeCheckedSyntax Target { get; set; }

        public ITypeCheckedSyntax Index { get; set; }

        public ArrayAccessKind AccessKind { get; set; }

        public CExpression GenerateCode(ICDeclarationWriter declWriter, ICStatementWriter statWriter) {
            var index = this.Index.GenerateCode(declWriter, statWriter);
            var target = this.Target.GenerateCode(declWriter, statWriter);
            var targetSize = CExpression.MemberAccess(target, "size");
            var targetData = CExpression.MemberAccess(target, "data");

            var cond = CExpression.BinaryExpression(
                CExpression.BinaryExpression(index, CExpression.IntLiteral(0), Primitives.BinaryOperation.LessThan),
                CExpression.BinaryExpression(index, targetSize, Primitives.BinaryOperation.GreaterThanOrEqualTo),
                Primitives.BinaryOperation.Or);

            var ifStat = CStatement.If(cond, new[] {
                CStatement.FromExpression(CExpression.Invoke(CExpression.VariableLiteral("fprintf"), new[] {
                    CExpression.VariableLiteral("stderr"),
                    CExpression.StringLiteral("array_out_of_bounds at " + this.Location.StartIndex + ":" + this.Location.Length) })),
                CStatement.FromExpression(CExpression.Invoke(CExpression.VariableLiteral("exit"), new[]{
                    CExpression.IntLiteral(-1) }))
            });

            statWriter.WriteStatement(ifStat);
            statWriter.WriteStatement(CStatement.NewLine());

            if (this.AccessKind == ArrayAccessKind.ValueAccess) {
                return CExpression.ArrayIndex(targetData, index);
            }
            else {
                return CExpression.BinaryExpression(targetData, index, Primitives.BinaryOperation.Add);
            }
        }
    }
}
