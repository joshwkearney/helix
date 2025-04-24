using Helix.Features.Structs;
using Helix.Features.Unions;
using Helix.Syntax;

namespace Helix.Parsing {
    public partial class Parser {
        private IDeclaration UnionDeclaration() {
            var start = this.Advance(TokenKind.UnionKeyword);
            var name = this.Advance(TokenKind.Identifier).Value;
            var mems = new List<ParseStructMember>();

            this.Advance(TokenKind.OpenBrace);

            while (!this.Peek(TokenKind.CloseBrace)) {
                var memStart = this.Advance(TokenKind.VarKeyword);              
                var memName = this.Advance(TokenKind.Identifier);
                this.Advance(TokenKind.AsKeyword);

                var memType = this.TopExpression();
                var memLoc = memStart.Location.Span(memType.Location);

                this.Advance(TokenKind.Semicolon);
                
                mems.Add(new ParseStructMember {
                    Location = memLoc,
                    MemberName = memName.Value,
                    MemberType = memType
                });
            }

            this.Advance(TokenKind.CloseBrace);
            var last = this.Advance(TokenKind.Semicolon);
            var loc = start.Location.Span(last.Location);

            var sig = new ParseStructSignature {
                Location = loc,
                Members = mems,
                Name = name
            };

            return new UnionParseDeclaration(loc, sig);
        }
    }
}