using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy.Features.Containers.Arrays {
    public class ArrayLiteralSyntaxA : ISyntaxA {
        private readonly IReadOnlyList<ISyntaxA> args;
        private readonly bool isreadonly;

        public TokenLocation Location { get; }

        public ArrayLiteralSyntaxA(TokenLocation location, bool isreadonly, IReadOnlyList<ISyntaxA> args) {
            this.Location = location;
            this.args = args;
            this.isreadonly = isreadonly;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            var args = this.args.Select(x => x.CheckNames(names)).ToArray();

            return new ArrayLiteralSyntaxB(this.Location, this.isreadonly, names.Context.Region, args);
        }
    }

    public class ArrayLiteralSyntaxB : ISyntaxB {
        private readonly IdentifierPath region;
        private readonly IReadOnlyList<ISyntaxB> args;
        private readonly bool isreadonly;

        public ArrayLiteralSyntaxB(TokenLocation location, bool isreadonly, IdentifierPath region, IReadOnlyList<ISyntaxB> args) {
            this.Location = location;
            this.region = region;
            this.args = args;
            this.isreadonly = isreadonly;
        }

        public TokenLocation Location { get; }

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage {
            get {
                return this.args
                    .Select(x => x.VariableUsage)
                    .Append(ImmutableDictionary.Create<IdentifierPath, VariableUsageKind>())
                    .Aggregate((x, y) => x.AddRange(y));
            }
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            var args = this.args.Select(x => x.CheckTypes(types)).ToArray();
            var returnType = new FixedArrayType(ITrophyType.Void, args.Length, this.isreadonly);

            // Make sure all the types line up
            if (args.Any()) {
                var expectedType = args.First().ReturnType;

                for (int i = 1; i < args.Length; i++) {
                    if (types.TryUnifyTo(args[i], expectedType).TryGetValue(out var newArg)) {
                        args[i] = newArg;
                    }
                    else {
                        throw TypeCheckingErrors.UnexpectedType(this.args[i].Location, expectedType, args[i].ReturnType);
                    }
                }

                returnType = new FixedArrayType(expectedType, args.Length, this.isreadonly);
            }

            return new ArrayLiteralSyntaxC(this.region, args, returnType);
        }
    }

    public class ArrayLiteralSyntaxC : ISyntaxC {
        private static int arrayTempCounter = 0;

        private readonly IReadOnlyList<ISyntaxC> args;
        private readonly IdentifierPath region;

        public ITrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes {
            get {
                var seed = ImmutableHashSet.Create<IdentifierPath>();

                return this.args
                    .Select(x => x.Lifetimes)
                    .Aggregate(seed, (x, y) => x.Union(y))
                    .Add(this.region);
            }
        }

        public ArrayLiteralSyntaxC(IdentifierPath regionName, IReadOnlyList<ISyntaxC> args, ITrophyType returnType) {
            this.args = args;
            this.region = regionName;
            this.ReturnType = returnType;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var args = this.args.Select(x => x.GenerateCode(declWriter, statWriter)).ToArray();
            var arrayName = "array_" + arrayTempCounter++;
            var arrayType = declWriter.ConvertType(this.ReturnType);
            var dataExpr = CExpression.MemberAccess(CExpression.VariableLiteral(arrayName), "data");
            var regionName = this.region.Segments.Last();

            // Write array declaration
            statWriter.WriteStatement(CStatement.Comment($"Array literal on region '{regionName}'"));
            statWriter.WriteStatement(CStatement.VariableDeclaration(arrayType, arrayName));

            if (!this.args.Any()) {
                // Write data assignment
                statWriter.WriteStatement(CStatement.Assignment(dataExpr, CExpression.IntLiteral(0)));
            }
            else if (RegionsHelper.IsStack(this.region)) {
                var elementType = declWriter.ConvertType(this.args.First().ReturnType);
                var cArrayName = "array_temp_" + arrayTempCounter++;
                var cArraySize = CExpression.IntLiteral(this.args.Count);

                // Write c array declaration
                statWriter.WriteStatement(CStatement.ArrayDeclaration(elementType, cArrayName, cArraySize));

                // Write data assignment
                statWriter.WriteStatement(CStatement.Assignment(
                    dataExpr,
                    CExpression.VariableLiteral(cArrayName)));
            }
            else {
                var elementType = declWriter.ConvertType(this.args.First().ReturnType);

                var count = CExpression.IntLiteral(this.args.Count);
                var elemSize = CExpression.Sizeof(elementType);
                var size = CExpression.BinaryExpression(count, elemSize, Primitives.BinaryOperation.Multiply);
                var arr = CExpression.Invoke(CExpression.VariableLiteral("region_alloc"), new[] {
                    CExpression.VariableLiteral(regionName),
                    size
                });

                arr = CExpression.Cast(CType.Pointer(elementType), arr);

                // Write data assignment
                statWriter.WriteStatement(CStatement.Assignment(dataExpr, arr));
            }

            // Write size assignment
            statWriter.WriteStatement(CStatement.Assignment(
                    CExpression.MemberAccess(CExpression.VariableLiteral(arrayName), "size"),
                    CExpression.IntLiteral(this.args.Count)));

            // Write array values
            for (int i = 0; i < this.args.Count; i++) {
                statWriter.WriteStatement(CStatement.Assignment(
                    CExpression.ArrayIndex(dataExpr, CExpression.IntLiteral(i)),
                    args[i]));
            }

            statWriter.WriteStatement(CStatement.NewLine());

            return CExpression.VariableLiteral(arrayName);
        }
    }
}