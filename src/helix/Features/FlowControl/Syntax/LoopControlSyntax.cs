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

        public bool AlwaysJumps => true;

        public HelixType ReturnType => PrimitiveType.Void;

        public bool IsPure => false;

        public TypeCheckResult CheckTypes(TypeFrame types) {
            if (this.Kind == LoopControlKind.Break) {
                types = types.WithBreakFrame(types);
            }
            else {
                types = types.WithContinueFrame(types);
            }

            return new TypeCheckResult(this, types);
        }

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
