using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;

namespace Attempt20.Features.Containers.Arrays {
    public class ArrayParsedLiteral : IParsedSyntax {
        private IdentifierPath region;

        public TokenLocation Location { get; set; }

        public IReadOnlyList<IParsedSyntax> Arguments { get; set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            this.Arguments = this.Arguments.Select(x => x.CheckNames(names)).ToArray();
            this.region = names.CurrentRegion;

            return this;
        }

        public ISyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            var args = this.Arguments.Select(x => x.CheckTypes(names, types)).ToArray();

            // Make sure all the types line up
            if (args.Any()) {
                var expectedType = args.First().ReturnType;

                for (int i = 1; i < args.Length; i++) {
                    if (types.TryUnifyTo(args[i], expectedType).TryGetValue(out var newArg)) {
                        args[i] = newArg;
                    }
                    else {
                        throw TypeCheckingErrors.UnexpectedType(args[i].Location, expectedType, args[i].ReturnType);
                    }
                }

                return new ArrayTypeCheckedLiteral() {
                    Arguments = args,
                    Lifetimes = args.Select(x => x.Lifetimes).Aggregate((x, y) => x.Union(y)).Add(this.region),
                    Location = this.Location,
                    ReturnType = new FixedArrayType(expectedType, args.Length),
                    RegionName = this.region.Segments.Last()
                };
            }
            else {
                return new ArrayTypeCheckedLiteral() {
                    Arguments = new ISyntax[0],
                    Lifetimes = ImmutableHashSet.Create<IdentifierPath>(),
                    Location = this.Location,
                    ReturnType = new ArrayType(TrophyType.Void),
                    RegionName = "stack"
                };
            }
        }
    }

    public class ArrayTypeCheckedLiteral : ISyntax {
        private static int arrayTempCounter = 0;

        public TokenLocation Location { get; set; }

        public TrophyType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }

        public IReadOnlyList<ISyntax> Arguments { get; set; }

        public string RegionName { get; set; }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var args = this.Arguments.Select(x => x.GenerateCode(declWriter, statWriter)).ToArray();
            var arrayName = "$array_" + arrayTempCounter++;
            var arrayType = declWriter.ConvertType(this.ReturnType);
            var dataExpr = CExpression.MemberAccess(CExpression.VariableLiteral(arrayName), "data");

            // Write array declaration
            statWriter.WriteStatement(CStatement.VariableDeclaration(arrayType, arrayName));

            if (!this.Arguments.Any()) {
                // Write data assignment
                statWriter.WriteStatement(CStatement.Assignment(dataExpr, CExpression.IntLiteral(0)));
            }
            else if (this.RegionName == "stack") {
                var elementType = declWriter.ConvertType(this.Arguments.First().ReturnType);
                var cArrayName = "$array_temp_" + arrayTempCounter++;
                var cArraySize = CExpression.IntLiteral(this.Arguments.Count);

                // Write c array declaration
                statWriter.WriteStatement(CStatement.ArrayDeclaration(elementType, cArrayName, cArraySize));

                // Write data assignment
                statWriter.WriteStatement(CStatement.Assignment(
                    dataExpr,
                    CExpression.VariableLiteral(cArrayName)));
            }
            else {
                var elementType = declWriter.ConvertType(this.Arguments.First().ReturnType);

                // Write data assignment
                statWriter.WriteStatement(CStatement.Assignment(
                    dataExpr,
                    CExpression.Invoke(CExpression.VariableLiteral("$region_alloc"), new[] {
                        CExpression.VariableLiteral(this.RegionName),
                        CExpression.BinaryExpression(
                            CExpression.IntLiteral(this.Arguments.Count),
                            CExpression.Sizeof(elementType),
                            Primitives.BinaryOperation.Multiply)
                    })));
            }

            // Write size assignment
            statWriter.WriteStatement(CStatement.Assignment(
                    CExpression.MemberAccess(CExpression.VariableLiteral(arrayName), "size"),
                    CExpression.IntLiteral(this.Arguments.Count)));

            // Write array values
            for (int i = 0; i < this.Arguments.Count; i++) {
                statWriter.WriteStatement(CStatement.Assignment(
                    CExpression.ArrayIndex(dataExpr, CExpression.IntLiteral(i)),
                    args[i]));
            }

            statWriter.WriteStatement(CStatement.NewLine());

            return CExpression.VariableLiteral(arrayName);
        }
    }
}