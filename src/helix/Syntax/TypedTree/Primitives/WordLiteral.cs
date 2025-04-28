using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Primitives {
    public record WordLiteral : IParseTree, ITypedTree {
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

        public Immediate GenerateIR(IRWriter writer, IRFrame context) => new Immediate.Word(this.Value);
    }
}
