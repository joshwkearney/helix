using System.Collections.Immutable;
using System.Linq;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Variables;
using Trophy.Parsing;

namespace Trophy.Features.Containers.Arrays {
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
            var region = (names.CurrentRegion == IdentifierPath.StackPath) ? IdentifierPath.HeapPath : names.CurrentRegion;
            var result = new ArrayLiteralAccessSyntaxB(this.Location, target, index, region);

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
        private readonly IdentifierPath region;

        public TokenLocation Location { get; }

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage {
            get {
                return this.target.VariableUsage.AddRange(this.index.VariableUsage);
            }
        }

        public ArrayLiteralAccessSyntaxB(TokenLocation location, ISyntaxB target, ISyntaxB index, IdentifierPath region) {
            this.Location = location;
            this.target = target;
            this.index = index;
            this.region = region;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var target = this.target.CheckTypes(types);
            var index = this.index.CheckTypes(types);
            var elementType = ITrophyType.Void;
            var isreadonly = false;

            // Make sure the target is an array
            if (target.ReturnType.AsArrayType().TryGetValue(out var arrayType)) {
                elementType = arrayType.ElementType;
                isreadonly = arrayType.IsReadOnly;
            }
            else if (target.ReturnType.AsFixedArrayType().TryGetValue(out var fixedArrayType)) {
                elementType = fixedArrayType.ElementType;
                isreadonly = fixedArrayType.IsReadOnly;
            }
            else {
                throw TypeCheckingErrors.ExpectedArrayType(this.target.Location, target.ReturnType);
            }

            // Make sure the index in an int
            if (types.TryUnifyTo(index, ITrophyType.Integer).TryGetValue(out var newIndex)) {
                index = newIndex;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(this.index.Location, ITrophyType.Integer, index.ReturnType);
            }

            return new ArrayLiteralAccessSyntaxC(target, index, new VarRefType(elementType, isreadonly), this.region);
        }
    }

    public class ArrayLiteralAccessSyntaxC : ISyntaxC {
        private readonly ISyntaxC target, index;
        private readonly IdentifierPath region;

        public ITrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.target.Lifetimes;

        public ArrayLiteralAccessSyntaxC(ISyntaxC target, ISyntaxC index, ITrophyType returnType, IdentifierPath region) {
            this.target = target;
            this.index = index;
            this.ReturnType = returnType;
            this.region = region;
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

            cond = CExpression.Invoke(CExpression.VariableLiteral("HEDLEY_UNLIKELY"), new[] { cond });

            var jump = CExpression.Invoke(
                CExpression.VariableLiteral("region_panic"),
                new[] { CExpression.VariableLiteral(this.region.Segments.Last()) });

            var ifStat = CStatement.If(cond, new[] { CStatement.FromExpression(jump) });

            statWriter.WriteStatement(CStatement.Comment("Array access bounds check"));
            statWriter.WriteStatement(ifStat);
            statWriter.WriteStatement(CStatement.NewLine());

            return CExpression.BinaryExpression(targetData, index, Primitives.BinaryOperation.Add);
        }
    }
}
