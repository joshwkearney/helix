using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Trophy.Features.Meta;

namespace Trophy.Features.Containers.Structs {
    public class NewArraySyntaxA : ISyntaxA {
        private readonly IReadOnlyList<StructArgument<ISyntaxA>> args;
        private readonly ITrophyType targetType;

        public TokenLocation Location { get; }

        public NewArraySyntaxA(TokenLocation location, ITrophyType targetType, IReadOnlyList<StructArgument<ISyntaxA>> args) {
            this.Location = location;
            this.targetType = targetType;
            this.args = args;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            // Make sure the target is an array
            if (!this.targetType.AsArrayType().TryGetValue(out var arrayType)) {
                throw TypeCheckingErrors.ExpectedStructType(this.Location, this.targetType);
            }

            // Check argument names
            var args = this.args
                .Select(x => new StructArgument<ISyntaxB>() {
                    MemberName = x.MemberName,
                    MemberValue = x.MemberValue.CheckNames(names)
                })
                .ToArray();

            // Throw errors if there are extra fields
            var extra = args.Select(x => x.MemberName).Where(x => x != "size").ToArray();
            if (extra.Any()) {
                throw TypeCheckingErrors.NewObjectHasExtraneousFields(this.Location, arrayType, extra);
            }

            var sizeField = args.Where(x => x.MemberName == "size").FirstOrDefault();
            if (sizeField == null) {
                return new AsSyntaxA(this.Location, new VoidLiteralAB(this.Location), new TypeAccessSyntaxA(this.Location, arrayType)).CheckNames(names);
            }

            var region = RegionsHelper.GetClosestHeap(names.Context.Region);

            return new NewArraySyntaxB(this.Location, region, arrayType, sizeField.MemberValue);
        }
    }

    public class NewArraySyntaxB : ISyntaxB {
        private readonly IdentifierPath region;
        private readonly ISyntaxB size;
        private readonly ArrayType arrayType;

        public TokenLocation Location { get; }

        public IImmutableSet<VariableUsage> VariableUsage {
            get => this.size.VariableUsage;
        }

        public NewArraySyntaxB(
            TokenLocation location, 
            IdentifierPath region,
            ArrayType arrayType, 
            ISyntaxB size) {

            this.Location = location;
            this.region = region;
            this.arrayType = arrayType;
            this.size = size;
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            // Check argument names
            var size = this.size.CheckTypes(types);

            // Make sure the size is an int
            if (!size.ReturnType.IsIntType) {
                throw TypeCheckingErrors.UnexpectedType(this.Location, ITrophyType.Integer, size.ReturnType);
            }

            // Make sure the inner type has a default value
            if (!this.arrayType.ElementType.HasDefaultValue(types)) {
                throw TypeCheckingErrors.TypeWithoutDefaultValue(this.Location, this.arrayType.ElementType);
            }

            return new NewArraySyntaxC(this.region, size, this.arrayType);
        }
    }

    public class NewArraySyntaxC : ISyntaxC {
        private static int tempCounter = 0;

        private readonly ISyntaxC size;
        private readonly IdentifierPath region;
        private readonly ArrayType arrayType;

        public ITrophyType ReturnType => this.arrayType;

        public NewArraySyntaxC(
            IdentifierPath region,
            ISyntaxC size, 
            ArrayType arrayType) {

            this.region = region;
            this.size = size;
            this.arrayType = arrayType;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var count = this.size.GenerateCode(declWriter, statWriter);
            var arrayName = "array_" + tempCounter++;
            var arrayType = declWriter.ConvertType(this.ReturnType);
            var regionName = this.region.Segments.Last();
            var dataExpr = CExpression.MemberAccess(CExpression.VariableLiteral(arrayName), "data");

            // Write array declaration
            statWriter.WriteStatement(CStatement.Comment($"New array on region '{regionName}'"));
            statWriter.WriteStatement(CStatement.VariableDeclaration(arrayType, arrayName));

            var elementType = declWriter.ConvertType(this.arrayType.ElementType);
            var elemSize = CExpression.Sizeof(elementType);
            var size = CExpression.BinaryExpression(count, elemSize, Primitives.BinaryOperation.Multiply);
            var arr = CExpression.Invoke(CExpression.VariableLiteral("region_alloc"), new[] {
                CExpression.VariableLiteral(regionName),
                size
            });

            arr = CExpression.Cast(CType.Pointer(elementType), arr);

            // Write data assignment
            statWriter.WriteStatement(CStatement.Assignment(dataExpr, arr));

            // Write size assignment
            statWriter.WriteStatement(CStatement.Assignment(
                    CExpression.MemberAccess(CExpression.VariableLiteral(arrayName), "size"),
                    count));

            statWriter.WriteStatement(CStatement.NewLine());

            return CExpression.VariableLiteral(arrayName);
        }
    }
}