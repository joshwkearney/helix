using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Features.Primitives;
using Attempt20.Parsing;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt20.Features.Containers.Arrays {
    public class NewFixedArraySyntax : IParsedSyntax {
        private IdentifierPath region;

        public FixedArrayType ArrayType { get; set; }

        public TokenLocation Location { get; set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            this.region = names.CurrentRegion;

            return this;
        }

        public ISyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            // Make sure that the element type has a default value
            if (!this.ArrayType.ElementType.HasDefaultValue(types)) {
                throw TypeCheckingErrors.TypeWithoutDefaultValue(this.Location, this.ArrayType.ElementType);
            }

            return new NewFixedArrayTypeCheckedSyntax() {
                ArrayType = this.ArrayType,
                Location = this.Location,
                RegionName = this.region.Segments.Last(),
                Lifetimes = new[] { this.region }.ToImmutableHashSet()
            };
        }
    }

    public class NewFixedArrayTypeCheckedSyntax : ISyntax {
        private int counter = 0;

        public FixedArrayType ArrayType { get; set; }

        public TokenLocation Location { get; set; }

        public string RegionName { get; set; }

        public TrophyType ReturnType => this.ArrayType;

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var arrayName = "$fixed_array_" + counter++;
            var arrayType = declWriter.ConvertType(this.ReturnType);
            var dataExpr = CExpression.MemberAccess(CExpression.VariableLiteral(arrayName), "data");
            var sizeExpr = CExpression.MemberAccess(CExpression.VariableLiteral(arrayName), "size");
            var elementType = declWriter.ConvertType(this.ArrayType.ElementType);

            // Write array declaration
            statWriter.WriteStatement(CStatement.VariableDeclaration(arrayType, arrayName));

            if (this.RegionName == "stack") {
                var cArrayName = "$array_temp_" + counter++;
                var cArraySize = CExpression.IntLiteral(this.ArrayType.Size);

                // Write c array declaration
                statWriter.WriteStatement(CStatement.ArrayDeclaration(elementType, cArrayName, cArraySize));

                // Write data assignment
                statWriter.WriteStatement(CStatement.Assignment(
                    dataExpr,
                    CExpression.VariableLiteral(cArrayName)));
            }
            else {
                // Write data assignment
                statWriter.WriteStatement(CStatement.Assignment(
                    dataExpr,
                    CExpression.Invoke(CExpression.VariableLiteral("$region_alloc"), new[] {
                        CExpression.VariableLiteral(this.RegionName),
                        CExpression.BinaryExpression(
                            CExpression.IntLiteral(this.ArrayType.Size),
                            CExpression.Sizeof(elementType),
                            Primitives.BinaryOperation.Multiply)
                    })));
            }

            // Write size assignment
            statWriter.WriteStatement(CStatement.Assignment(sizeExpr, CExpression.IntLiteral(this.ArrayType.Size)));

            // Memset the data
            statWriter.WriteStatement(CStatement.FromExpression(
                CExpression.Invoke(
                    CExpression.VariableLiteral("memset"),
                    new[] { 
                        dataExpr,
                        CExpression.IntLiteral(0),
                        CExpression.BinaryExpression(
                            CExpression.IntLiteral(this.ArrayType.Size),
                            CExpression.Sizeof(elementType),
                        BinaryOperation.Multiply)
                    })));

            statWriter.WriteStatement(CStatement.NewLine());

            return CExpression.VariableLiteral(arrayName);
        }
    }
}
