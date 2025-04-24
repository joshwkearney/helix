using Helix.Analysis;
using Helix.Analysis.Predicates;
using Helix.Analysis.Types;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;

namespace Helix.Features.Unions {

    public class NewUnionSyntax : ISyntax {
        public required TokenLocation Location { get; init; }

        public required HelixType ReturnType { get; init; }
        
        public required UnionType Signature { get; init; }
        
        public required string Name { get; init; }
        
        public required ISyntax Value { get; init; }
        
        public ISyntaxPredicate Predicate => ISyntaxPredicate.Empty;

        public ISyntax ToRValue(TypeFrame types) => this;

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var value = this.Value.GenerateCode(types, writer);

            var unionStructType = writer.ConvertType(this.Signature, types);
            var unionUnionType = new CNamedType(unionStructType.WriteToC() + "_$Union");
            var index = this.Signature.Members.IndexOf(x => x.Name == this.Name);

            return new CCompoundExpression() {
                Type = unionStructType,
                MemberNames = new[] { "tag", "data" },
                Arguments = new ICSyntax[] { 
                    new CIntLiteral(index),
                    new CCompoundExpression() {
                        MemberNames = new[] { this.Name },
                        Arguments = new[] { value }
                    }
                },
            };
        }
    }
}