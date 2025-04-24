using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.Arrays;
using Helix.Parsing;
using Helix.Features.Variables;

namespace Helix.Parsing {
    public partial class Parser {
        public IParseSyntax ArrayExpression(IParseSyntax start) {
            this.Advance(TokenKind.OpenBracket);

            if (this.Peek(TokenKind.CloseBracket)) {
                var end = this.Advance(TokenKind.CloseBracket);
                var loc = start.Location.Span(end.Location);

                return new ArrayTypeParse(loc, start);
            }
            else {
                var index = this.TopExpression();
                var end = this.Advance(TokenKind.CloseBracket);
                var loc = start.Location.Span(end.Location);

                return new ArrayIndexParse(loc, start, index);
            }            
        }
    }
}

namespace Helix.Features.Arrays {
    public record ArrayIndexParse : IParseSyntax {
        private readonly IParseSyntax target;
        private readonly IParseSyntax index;

        public TokenLocation Location { get; }

        public IEnumerable<IParseSyntax> Children => new[] { this.target, this.index };

        public bool IsPure { get; }

        public ArrayIndexParse(TokenLocation loc, IParseSyntax target, IParseSyntax index) {
            this.Location = loc;
            this.target = target;
            this.index = index;

            this.IsPure = this.target.IsPure && this.index.IsPure;
        }

        public IParseSyntax CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var target = this.target
                .CheckTypes(types)
                .ToRValue(types);

            var index = this.index
                .CheckTypes(types)
                .ToRValue(types)
                .UnifyTo(PrimitiveType.Word, types);

            // Make sure we have an array
            if (target.GetReturnType(types) is not ArrayType arrayType) {
                throw TypeException.ExpectedArrayType(
                    this.target.Location, 
                    target.GetReturnType(types));
            }

            var adapter = new ArrayToPointerAdapter(arrayType, target, index);

            var deref = new DereferenceParseParse(
                this.Location,
                adapter);

            return deref.CheckTypes(types);
        }
    }
}