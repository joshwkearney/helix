using Helix.Analysis;
using Helix.Analysis.Predicates;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Unions {

    public record IsSyntax : ISyntax {
        public required TokenLocation Location { get; init; }

        public required HelixType ReturnType { get; init; }
        
        public required IdentifierPath VariablePath { get; init; }

        public required string MemberName { get; init; }

        public required UnionType UnionSignature { get; init; }
        
        public ISyntaxPredicate Predicate => ISyntaxPredicate.Empty;
        
        public ISyntax ToRValue(TypeFrame types) => this;

        public ICSyntax GenerateCode(TypeFrame flow, ICStatementWriter writer) {
            var varName = writer.GetVariableName(this.VariablePath);

            var index = this.UnionSignature
                .Members
                .Select(x => x.Name)
                .IndexOf(x => x == this.MemberName);

            return new CBinaryExpression() {
                Operation = Primitives.BinaryOperationKind.EqualTo,
                Left = new CMemberAccess() {
                    Target = new CVariableLiteral(varName),
                    MemberName = "tag"
                },
                Right = new CIntLiteral(index)
            };
        }
    }
}