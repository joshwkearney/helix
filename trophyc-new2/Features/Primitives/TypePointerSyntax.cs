using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Analysis;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Primitives;
using Trophy.Parsing;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree TypePointer(ISyntaxTree start) {
            TokenLocation loc;
            bool isWritable;

            if (this.Peek(TokenKind.Multiply)) {
                loc = this.Advance(TokenKind.Multiply).Location;
                isWritable = true;
            }
            else {
                loc = this.Advance(TokenKind.Caret).Location;
                isWritable = false;
            }

            loc = start.Location.Span(loc);

            return new TypePointerSyntax(loc, start, isWritable);
        }
    }
}

namespace Trophy.Features.Primitives {
    public record TypePointerSyntax : ISyntaxTree {
        private readonly ISyntaxTree inner;
        private readonly bool isWritable;

        public TokenLocation Location { get; }

        public TypePointerSyntax(TokenLocation loc, ISyntaxTree inner, bool isWritable) {
            this.Location = loc;
            this.inner = inner;
            this.isWritable = isWritable;
        }

        public Option<TrophyType> ToType(INamesObserver types, IdentifierPath currentScope) {
            return this.inner.ToType(types, currentScope)
                .Select(x => new PointerType(x, this.isWritable))
                .Select(x => (TrophyType)x);
        }

        public ISyntaxTree CheckTypes(INamesObserver names, ITypesRecorder types) => this;

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) => Option.None;

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) => Option.None;

        public CExpression GenerateCode(CStatementWriter statWriter) {
            throw new InvalidOperationException();
        }
    }
}
