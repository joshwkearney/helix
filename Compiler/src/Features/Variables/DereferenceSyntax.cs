using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;
using System.Collections.Immutable;

namespace Trophy.Features.Variables {
    public class DereferenceSyntaxB : ISyntaxB {
        private readonly ISyntaxB target;

        public TokenLocation Location => this.target.Location;

        public DereferenceSyntaxB(ISyntaxB target) {
            this.target = target;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var target = this.target.CheckTypes(types);
            var lifetimes = target.Lifetimes;

            // Make sure the target is a variable type
            if (!target.ReturnType.AsVariableType().TryGetValue(out var varType)) {
                throw TypeCheckingErrors.ExpectedVariableType(this.Location, target.ReturnType);
            }

            // Clear the lifetimes if the returned type is a pure value type
            if (varType.InnerType.GetCopiability(types) == TypeCopiability.Unconditional) {
                lifetimes = lifetimes.Clear();
            }

            return new DereferenceSyntaxC(target, varType.InnerType, lifetimes);
        }
    }

    public class DereferenceSyntaxC : ISyntaxC {
        private readonly ISyntaxC target;

        public TrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; }

        public DereferenceSyntaxC(ISyntaxC target, TrophyType returnType, ImmutableHashSet<IdentifierPath> lifetimes) {
            this.target = target;
            this.ReturnType = returnType;
            this.Lifetimes = lifetimes;
        }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter) {
            var target = this.target.GenerateCode(writer, statWriter);

            return CExpression.Dereference(target);
        }
    }
}