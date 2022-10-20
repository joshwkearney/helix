using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Generation.Syntax;
using Trophy.Parsing;

namespace Trophy.Features.Primitives {
    public record DereferenceSyntax : ISyntaxTree {
        private readonly ISyntaxTree target;
        private readonly bool isTypeChecked;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public DereferenceSyntax(TokenLocation loc, ISyntaxTree target, bool isTypeChecked = false) {
            this.Location = loc;
            this.target = target;
            this.isTypeChecked = isTypeChecked;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            var target = this.target.CheckTypes(types);
            var pointerType = target.AssertIsPointer(types);
            var result = new DereferenceSyntax(this.Location, target, true);

            types.ReturnTypes[result] = pointerType.InnerType;
            return result;
        }

        public ISyntaxTree ToLValue(SyntaxFrame types) {
            if (!this.isTypeChecked) {
                throw TypeCheckingErrors.LValueRequired(this.Location);
            }

            var pointerType = this.target.AssertIsPointer(types);
            if (!pointerType.IsWritable) {
                throw TypeCheckingErrors.WritingToConstPointer(this.Location);
            }

            return this.target;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            if (!this.isTypeChecked) {
                throw TypeCheckingErrors.RValueRequired(this.Location);
            }

            return this;
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            return new CPointerDereference() {
                Target = this.target.GenerateCode(writer)
            };
        }
    }
}
