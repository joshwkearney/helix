using System.Collections.Immutable;
using System.Linq;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.FlowControl;
using Trophy.Features.Primitives;
using Trophy.Features.Variables;
using Trophy.Parsing;

namespace Trophy.Features.Containers.Arrays {
    public class ArraySliceSyntaxA : ISyntaxA {
        private readonly ISyntaxA target;
        private readonly IOption<ISyntaxA> startIndex, endIndex;

        public ArraySliceSyntaxA(
            TokenLocation location, 
            ISyntaxA target, 
            IOption<ISyntaxA> startIndex, 
            IOption<ISyntaxA> endIndex) {

            this.Location = location;
            this.target = target;
            this.startIndex = startIndex;
            this.endIndex = endIndex;
        }

        public TokenLocation Location { get; set; }

        public ISyntaxB CheckNames(INameRecorder names) {
            var target = this.target;
            var start = this.startIndex.GetValueOr(() => new IntLiteralSyntax(this.Location, 0));
            var region = names.CurrentRegion == IdentifierPath.StackPath ? IdentifierPath.HeapPath : names.CurrentRegion;
            
            // If end is defined then move on, otherwise rewrite it to access target.size
            if (this.endIndex.TryGetValue(out var end)) {
                return new ArraySliceSyntaxB(
                    this.Location, 
                    target.CheckNames(names), 
                    start.CheckNames(names), 
                    end.CheckNames(names),
                    region);
            }
            else {
                var tempName = "$slice_temp" + names.GetNewVariableId();
                var letSyntax = new VarRefSyntaxA(this.Location, tempName, target, false);
                var accessSyntax = new IdentifierAccessSyntaxA(this.Location, tempName, VariableAccessKind.ValueAccess);

                return new BlockSyntaxA(this.Location, new ISyntaxA[] {
                    letSyntax,
                    new ArraySliceSyntaxA(
                        this.Location,
                        accessSyntax,
                        Option.Some(start),
                        Option.Some(new MemberAccessSyntaxA(this.Location, accessSyntax, "size")))
                })
                .CheckNames(names);
            }
        }
    }

    public class ArraySliceSyntaxB : ISyntaxB {
        private readonly ISyntaxB target, startIndex, endIndex;
        private readonly IdentifierPath region;

        public TokenLocation Location { get; }

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage {
            get => this.target.VariableUsage
                .AddRange(this.startIndex.VariableUsage)
                .AddRange(this.endIndex.VariableUsage);
        }

        public ArraySliceSyntaxB(TokenLocation location, ISyntaxB target, ISyntaxB startIndex, ISyntaxB endIndex, IdentifierPath region) {
            this.Location = location;
            this.target = target;
            this.startIndex = startIndex;
            this.endIndex = endIndex;
            this.region = region;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var target = this.target.CheckTypes(types);
            var startIndex = this.startIndex.CheckTypes(types);
            var endIndex = this.endIndex.CheckTypes(types);
            var returnType = ITrophyType.Void;

            // Make sure the target is an array
            if (target.ReturnType.AsArrayType().TryGetValue(out var arrayType)) {
                returnType = arrayType;
            }
            else if (target.ReturnType.AsFixedArrayType().TryGetValue(out var fixedArrayType)) {
                returnType = new ArrayType(fixedArrayType.ElementType, fixedArrayType.IsReadOnly);
            }
            else {
                throw TypeCheckingErrors.ExpectedArrayType(this.target.Location, target.ReturnType);
            }

            // Make sure the start index in an int
            if (!types.TryUnifyTo(startIndex, ITrophyType.Integer).TryGetValue(out startIndex)) {
                throw TypeCheckingErrors.UnexpectedType(this.startIndex.Location, ITrophyType.Integer, startIndex.ReturnType);
            }

            // Make sure the end index in an int
            if (!types.TryUnifyTo(endIndex, ITrophyType.Integer).TryGetValue(out endIndex)) {
                throw TypeCheckingErrors.UnexpectedType(this.endIndex.Location, ITrophyType.Integer, endIndex.ReturnType);
            }

            return new ArraySliceSyntaxC(returnType, target, startIndex, endIndex, this.region);
        }
    }
    public class ArraySliceSyntaxC : ISyntaxC {
        private static int sliceCounter = 0;
        private IdentifierPath region;

        private readonly ISyntaxC target, start, end;

        public ITrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.target.Lifetimes;

        public ArraySliceSyntaxC(ITrophyType returnType, ISyntaxC target, ISyntaxC start, ISyntaxC end, IdentifierPath region) {
            this.ReturnType = returnType;
            this.target = target;
            this.start = start;
            this.end = end;
            this.region = region;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var target = this.target.GenerateCode(declWriter, statWriter);
            var sizeExpr = CExpression.MemberAccess(target, "size");

            var start = this.start.GenerateCode(declWriter, statWriter);
            var end = this.end.GenerateCode(declWriter, statWriter);

            var cond = CExpression.BinaryExpression(
                CExpression.BinaryExpression(start, CExpression.IntLiteral(0), BinaryOperation.LessThan),
                CExpression.BinaryExpression(
                    CExpression.BinaryExpression(end, start, BinaryOperation.LessThanOrEqualTo),
                    CExpression.BinaryExpression(end, sizeExpr, BinaryOperation.GreaterThan),
                    BinaryOperation.Or),
                BinaryOperation.Or);

            cond = CExpression.Invoke(CExpression.VariableLiteral("HEDLEY_UNLIKELY"), new[] { cond });

            var jump = CExpression.Invoke(
                CExpression.VariableLiteral("region_panic"),
                new[] { CExpression.VariableLiteral(this.region.Segments.Last()) });

            var ifStat = CStatement.If(cond, new[] { CStatement.FromExpression(jump) });

            statWriter.WriteStatement(CStatement.Comment("Array slice bounds check"));
            statWriter.WriteStatement(ifStat);
            statWriter.WriteStatement(CStatement.NewLine());

            var arrayName = "array_slice_" + sliceCounter++;
            var arrayType = declWriter.ConvertType(this.ReturnType);

            statWriter.WriteStatement(CStatement.Comment("Array slice"));
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