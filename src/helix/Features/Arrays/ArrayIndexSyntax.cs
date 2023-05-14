using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helix.Analysis;
using Helix.Analysis.Lifetimes;
using Helix.Analysis.Types;
using Helix.Features.Arrays;
using Helix.Features.Memory;
using Helix.Features.Primitives;
using Helix.Features.Variables;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Parsing {
    public partial class Parser {
        public ISyntaxTree ArrayExpression(ISyntaxTree start) {
            this.Advance(TokenKind.OpenBracket);

            if (this.Peek(TokenKind.CloseBracket)) {
                var end = this.Advance(TokenKind.CloseBracket);
                var loc = start.Location.Span(end.Location);

                return new ArrayTypeSyntax(loc, start, true);
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
        private static int tempCounter = 0;

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

        public ISyntaxTree CheckTypes(EvalFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var target = this.target
                .CheckTypes(types)
                .ToRValue(types);

            var index = this.index
                .CheckTypes(types)
                .ToRValue(types)
                .ConvertTo(PrimitiveType.Int, types);

            // Make sure we have an array
            if (types.ReturnTypes[target] is not ArrayType arrayType) {
                throw TypeCheckingErrors.ExpectedArrayType(
                    this.target.Location, 
                    types.ReturnTypes[target]);
            }

            var adapter = new ArrayToPointerAdapter(arrayType, target, index);

            var deref = new DereferenceSyntax(
                this.Location,
                adapter,
                this.Location.Scope.Append("$array_index_" + tempCounter++));

            return deref.CheckTypes(types);
        }
    }
}