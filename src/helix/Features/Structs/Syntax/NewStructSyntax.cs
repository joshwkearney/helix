using Helix.Analysis;
using Helix.Analysis.Predicates;
using Helix.Analysis.Types;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;

namespace Helix.Features.Structs {
    public class NewStructSyntax : ISyntax {
        public required TokenLocation Location { get; init; }
        
        public required StructType Signature { get; init; }
        
        public required IReadOnlyList<string> Names { get; init; }
        
        public required IReadOnlyList<ISyntax> Values { get; init; }

        public HelixType ReturnType => this.Signature;

        public ISyntaxPredicate Predicate => ISyntaxPredicate.Empty;

        public ISyntax ToRValue(TypeFrame types) {
            return this;
        }
        
        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var memValues = this.Values
                .Select(x => x.GenerateCode(types, writer))
                .ToArray();

            if (!memValues.Any()) {
                memValues = new[] { new CIntLiteral(0) };
            }

            return new CCompoundExpression() {
                Type = writer.ConvertType(this.Signature, types),
                MemberNames = this.Names,
                Arguments = memValues,
            };
        }
    }
}