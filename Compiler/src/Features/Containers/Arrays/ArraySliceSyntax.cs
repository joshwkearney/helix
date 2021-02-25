using System.Collections.Immutable;
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
            
            // If end is defined then move on, otherwise rewrite it to access target.size
            if (this.endIndex.TryGetValue(out var end)) {
                return new ArraySliceSyntaxB(
                    this.Location, 
                    target.CheckNames(names), 
                    start.CheckNames(names), 
                    end.CheckNames(names));
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

        public TokenLocation Location { get; }

        public ArraySliceSyntaxB(TokenLocation location, ISyntaxB target, ISyntaxB startIndex, ISyntaxB endIndex) {
            this.Location = location;
            this.target = target;
            this.startIndex = startIndex;
            this.endIndex = endIndex;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var target = this.target.CheckTypes(types);
            var startIndex = this.startIndex.CheckTypes(types);
            var endIndex = this.endIndex.CheckTypes(types);
            var returnType = TrophyType.Void;

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
            if (!types.TryUnifyTo(startIndex, TrophyType.Integer).TryGetValue(out startIndex)) {
                throw TypeCheckingErrors.UnexpectedType(this.startIndex.Location, TrophyType.Integer, startIndex.ReturnType);
            }

            // Make sure the end index in an int
            if (!types.TryUnifyTo(endIndex, TrophyType.Integer).TryGetValue(out endIndex)) {
                throw TypeCheckingErrors.UnexpectedType(this.endIndex.Location, TrophyType.Integer, endIndex.ReturnType);
            }

            return new ArraySliceSyntaxC(returnType, target, startIndex, endIndex);
        }
    }
    public class ArraySliceSyntaxC : ISyntaxC {
        private static int sliceCounter = 0;

        private readonly ISyntaxC target, start, end;

        public TrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.target.Lifetimes;

        public ArraySliceSyntaxC(TrophyType returnType, ISyntaxC target, ISyntaxC start, ISyntaxC end) {
            this.ReturnType = returnType;
            this.target = target;
            this.start = start;
            this.end = end;
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

            var ifStat = CStatement.If(cond, new[] {
                CStatement.FromExpression(CExpression.Invoke(CExpression.VariableLiteral("fprintf"), new[] {
                    CExpression.VariableLiteral("stderr"),
                    CExpression.StringLiteral("array_slice_out_of_bounds") })),
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