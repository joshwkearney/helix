using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Generation.Syntax;
using Trophy.Parsing;

namespace Trophy.Features.Primitives {
    public record DereferenceSyntax : ISyntax {
        private readonly ISyntax target;
        private readonly bool isTypeChecked;

        public TokenLocation Location { get; }

        public DereferenceSyntax(TokenLocation loc, ISyntax target, bool isTypeChecked = false) {
            this.Location = loc;
            this.target = target;
            this.isTypeChecked = isTypeChecked;
        }

        public Option<TrophyType> TryInterpret(INamesRecorder names) => Option.None;

        public ISyntax CheckTypes(ITypesRecorder types) {
            var target = this.target.CheckTypes(types);
            var pointerType = types.AssertIsPointer(target);
            var result = new DereferenceSyntax(this.Location, target, true);

            types.SetReturnType(result, pointerType.ReferencedType);
            return result;
        }

        public ISyntax ToLValue(ITypesRecorder types) {
            if (!this.isTypeChecked) {
                throw TypeCheckingErrors.LValueRequired(this.Location);
            }

            var pointerType = types.AssertIsPointer(this.target);
            if (!pointerType.IsWritable) {
                throw TypeCheckingErrors.WritingToConstPointer(this.Location);
            }

            return this.target;
        }

        public ISyntax ToRValue(ITypesRecorder types) {
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
