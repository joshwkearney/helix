using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.IRGeneration;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Primitives.Syntax {
    public record WordLiteral : IParseSyntax, ISyntax {
        public required TokenLocation Location { get; init; }
        
        public required long Value { get; init; }

        public bool AlwaysJumps => false;
        
        public HelixType ReturnType => new SingularWordType(this.Value);

        public bool IsPure => true;

        public Option<HelixType> AsType(TypeFrame types) {
            return new SingularWordType(this.Value);
        }

        public TypeCheckResult CheckTypes(TypeFrame types) => new(this, types);

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            return new CIntLiteral(this.Value);
        }

        public Immediate GenerateIR(IRWriter writer, IRFrame context, Immediate? returnName) => new Immediate.Word(this.Value);
    }
}
