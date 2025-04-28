using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Structs {
    public class NewStructTypedTree : ITypedTree {
        public required TokenLocation Location { get; init; }
        
        public required StructType StructSignature { get; init; }
        
        public required HelixType StructType { get; init; }
        
        public required IReadOnlyList<string> Names { get; init; }
        
        public required IReadOnlyList<ITypedTree> Values { get; init; }
        
        public required bool AlwaysJumps { get; init; }

        public HelixType ReturnType => this.StructType;

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var memValues = this.Values
                .Select(x => x.GenerateCode(types, writer))
                .ToArray();

            if (memValues.Length == 0) {
                memValues = [new CIntLiteral(0)];
            }

            return new CCompoundExpression() {
                Type = writer.ConvertType(this.StructType, types),
                MemberNames = this.Names,
                Arguments = memValues,
            };
        }
    }
}