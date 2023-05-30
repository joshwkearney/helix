using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.Arrays;
using Helix.Parsing;
using Helix.Features.Variables;

namespace Helix.Parsing {
    public partial class Parser {
        public ISyntaxTree ArrayExpression(ISyntaxTree start) {
            this.Advance(TokenKind.OpenBracket);

            if (this.Peek(TokenKind.CloseBracket)) {
                var end = this.Advance(TokenKind.CloseBracket);
                var loc = start.Location.Span(end.Location);

                return new ArrayTypeSyntax(loc, start);
            }
            else {
                var index = this.TopExpression();
                var end = this.Advance(TokenKind.CloseBracket);
                var loc = start.Location.Span(end.Location);

                return new ArrayIndexSyntax(loc, start, index);
            }            
        }
    }
}

namespace Helix.Features.Arrays {
    public record ArrayIndexSyntax : ISyntaxTree {
        private readonly ISyntaxTree target;
        private readonly ISyntaxTree index;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target, this.index };

        public bool IsPure { get; }

        public ArrayIndexSyntax(TokenLocation loc, ISyntaxTree target, ISyntaxTree index) {
            this.Location = loc;
            this.target = target;
            this.index = index;

            this.IsPure = this.target.IsPure && this.index.IsPure;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var target = this.target
                .CheckTypes(types)
                .ToRValue(types);

            var index = this.index
                .CheckTypes(types)
                .ToRValue(types)
                .UnifyTo(PrimitiveType.Int, types);

            // Make sure we have an array
            if (target.GetReturnType(types) is not ArrayType arrayType) {
                throw TypeException.ExpectedArrayType(
                    this.target.Location, 
                    target.GetReturnType(types));
            }

            var adapter = new ArrayToPointerAdapter(arrayType, target, index);

            var deref = new DereferenceParseSyntax(
                this.Location,
                adapter);

            return deref.CheckTypes(types);
        }
    }
}