using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public Option<TrophyType> ToType(INamesRecorder names) => Option.None;

        public ISyntax CheckTypes(ITypesRecorder types) {
            var target = this.target.CheckTypes(types);
            var pointerType = types.AssertIsPointer(target);
            var result = new DereferenceSyntax(this.Location, target, true);

            types.SetReturnType(result, pointerType.ReferencedType);
            return result;
        }

        public Option<ISyntax> ToLValue(ITypesRecorder types) {
            if (!this.isTypeChecked) {
                return Option.None;
            }

            var pointerType = types.AssertIsPointer(this.target);
            if (!pointerType.IsWritable) {
                return Option.None;
            }

            return Option.Some(this.target);
        }

        public Option<ISyntax> ToRValue(ITypesRecorder types) {
            return this.isTypeChecked ? this : Option.None;
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            return new CPointerDereference() {
                Target = this.target.GenerateCode(writer)
            };
        }
    }
}
