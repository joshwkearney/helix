using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Features.Primitives;
using Attempt20.Parsing;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt20.Features.Containers.Arrays {
    public class NewFixedArraySyntaxA : ISyntaxA {
        private readonly FixedArrayType arrayType;

        public TokenLocation Location { get; }

        public NewFixedArraySyntaxA(TokenLocation loc, FixedArrayType type) {
            this.Location = loc;
            this.arrayType = type;
        }

        public ISyntaxB CheckNames(INameRecorder names) {
            var region = names.CurrentRegion;

            return new NewFixedArraySyntaxBC(this.Location, this.arrayType, region);
        }

        public class NewFixedArraySyntaxBC : ISyntaxB, ISyntaxC {
            private static int counter;

            private readonly FixedArrayType arrayType;
            private readonly IdentifierPath region;

            public TokenLocation Location { get; }

            public TrophyType ReturnType => this.arrayType;

            public ImmutableHashSet<IdentifierPath> Lifetimes => new[] { this.region }.ToImmutableHashSet();

            public NewFixedArraySyntaxBC(TokenLocation location, FixedArrayType arrayType, IdentifierPath region) {
                this.Location = location;
                this.arrayType = arrayType;
                this.region = region;
            }

            public ISyntaxC CheckTypes(ITypeRecorder types) {
                // Make sure that the element type has a default value
                if (!this.arrayType.ElementType.HasDefaultValue(types)) {
                    throw TypeCheckingErrors.TypeWithoutDefaultValue(this.Location, this.arrayType.ElementType);
                }

                return this;
            }

            public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter) {
                var arrayName = "$fixed_array_" + counter++;
                var arrayType = writer.ConvertType(this.ReturnType);
                var dataExpr = CExpression.MemberAccess(CExpression.VariableLiteral(arrayName), "data");
                var sizeExpr = CExpression.MemberAccess(CExpression.VariableLiteral(arrayName), "size");
                var elementType = writer.ConvertType(this.arrayType.ElementType);

                // Write array declaration
                statWriter.WriteStatement(CStatement.VariableDeclaration(arrayType, arrayName));

                if (this.region == IdentifierPath.StackPath) {
                    var cArrayName = "$array_temp_" + counter++;
                    var cArraySize = CExpression.IntLiteral(this.arrayType.Size);

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
                        CExpression.VariableLiteral(this.region.Segments.Last()),
                        CExpression.BinaryExpression(
                            CExpression.IntLiteral(this.arrayType.Size),
                            CExpression.Sizeof(elementType),
                            Primitives.BinaryOperation.Multiply)
                        })));
                }

                // Write size assignment
                statWriter.WriteStatement(CStatement.Assignment(sizeExpr, CExpression.IntLiteral(this.arrayType.Size)));

                // Memset the data
                statWriter.WriteStatement(CStatement.FromExpression(
                    CExpression.Invoke(
                        CExpression.VariableLiteral("memset"),
                        new[] {
                        dataExpr,
                        CExpression.IntLiteral(0),
                        CExpression.BinaryExpression(
                            CExpression.IntLiteral(this.arrayType.Size),
                            CExpression.Sizeof(elementType),
                        BinaryOperation.Multiply)
                        })));

                statWriter.WriteStatement(CStatement.NewLine());

                return CExpression.VariableLiteral(arrayName);
            }
        }
    }
}
