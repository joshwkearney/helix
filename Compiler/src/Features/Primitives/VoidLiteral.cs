using System.Collections.Immutable;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;

namespace Attempt20.Features.Primitives {
    public class VoidLiteralAB : ISyntaxA, ISyntaxB {
        public TokenLocation Location { get; }

        public VoidLiteralAB(TokenLocation loc) {
            this.Location = loc;
        }

        public ISyntaxB CheckNames(INameRecorder names) {
            return this;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            return new VoidLiteralC();
        }
    }

    public class VoidLiteralC : ISyntaxC {
        public TrophyType ReturnType => TrophyType.Void;

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            return CExpression.IntLiteral(0);
        }

        public override string ToString() {
            return "void";
        }
    }
}