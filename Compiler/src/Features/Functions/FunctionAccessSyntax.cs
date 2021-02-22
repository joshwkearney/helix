using System.Collections.Immutable;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;

namespace Attempt20.Features.Functions {
    public class FunctionAccessSyntaxBC : ISyntaxB, ISyntaxC {
        private readonly IdentifierPath path;

        public TokenLocation Location { get; }

        public TrophyType ReturnType => new NamedType(this.path);

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public FunctionAccessSyntaxBC(TokenLocation loc, IdentifierPath path) {
            this.Location = loc;
            this.path = path;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            return this;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            return CExpression.IntLiteral(0);
        }
    }
}
