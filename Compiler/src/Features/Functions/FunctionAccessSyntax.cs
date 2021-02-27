using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy.Features.Functions {
    public class FunctionAccessSyntaxBC : ISyntaxB, ISyntaxC {
        private readonly IdentifierPath path;

        public TokenLocation Location { get; }

        public TrophyType ReturnType => new SingularFunctionType(this.path);

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage {
            get => ImmutableDictionary.Create<IdentifierPath, VariableUsageKind>();
        }

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
