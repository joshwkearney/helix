using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;
using System.Collections.Immutable;

namespace Trophy.Features.Variables {
    public class DereferenceSyntaxB : ISyntaxB {
        private readonly ISyntaxB target;

        public TokenLocation Location => this.target.Location;

        public IImmutableSet<VariableUsage> VariableUsage {
            get => this.target.VariableUsage;
        }

        public DereferenceSyntaxB(ISyntaxB target) {
            this.target = target;
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            var target = this.target.CheckTypes(types);

            // Make sure the target is a variable type
            if (!target.ReturnType.AsVariableType().TryGetValue(out var varType)) {
                throw TypeCheckingErrors.ExpectedVariableType(this.Location, target.ReturnType);
            }

            return new DereferenceSyntaxC(target, varType.InnerType);
        }
    }

    public class DereferenceSyntaxC : ISyntaxC {
        private readonly ISyntaxC target;

        public ITrophyType ReturnType { get; }

        public DereferenceSyntaxC(
            ISyntaxC target, 
            ITrophyType returnType) {

            this.target = target;
            this.ReturnType = returnType;
        }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter) {
            var target = this.target.GenerateCode(writer, statWriter);

            return CExpression.Dereference(target);
        }
    }
}