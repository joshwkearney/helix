using Helix.Analysis.Predicates;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Syntax;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Features.FlowControl {
    public record LoopControlSyntax : IParseSyntax, ISyntax {
        public required LoopControlKind Kind { get; init; }

        public required TokenLocation Location { get; init; }

        public HelixType ReturnType => PrimitiveType.Void;

        public ISyntaxPredicate Predicate => ISyntaxPredicate.Empty;

        public bool IsPure => false;

        public ISyntax CheckTypes(TypeFrame types) => this;

        public ISyntax ToRValue(TypeFrame types) => this;

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            if (this.Kind == LoopControlKind.Break) {
                writer.WriteStatement(new CBreak());
            }
            else {
                writer.WriteStatement(new CContinue());
            }

            return new CIntLiteral(0);
        }
    }
}
