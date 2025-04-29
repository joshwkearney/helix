using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Unions {

    public class NewUnionTypedTree : ITypedTree {
        public required TokenLocation Location { get; init; }
        
        public required HelixType UnionType { get; init; }
        
        public required UnionType UnionSignature { get; init; }
        
        public required string Name { get; init; }
        
        public required ITypedTree Value { get; init; }
        
        public HelixType ReturnType => this.UnionType;

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var value = this.Value.GenerateCode(types, writer);

            var unionStructType = writer.ConvertType(this.UnionType, types);
            var unionUnionType = new CNamedType(unionStructType.WriteToC() + "_$Union");
            var index = this.UnionSignature.Members.IndexOf(x => x.Name == this.Name);

            return new CCompoundExpression {
                Type = unionStructType,
                MemberNames = new[] { "tag", "data" },
                Arguments = new ICSyntax[] { 
                    new CIntLiteral(index),
                    new CCompoundExpression {
                        MemberNames = new[] { this.Name },
                        Arguments = new[] { value }
                    }
                },
            };
        }
    }
}