using Helix.Features.Structs;
using Helix.Features.Structs.ParseSyntax;
using Helix.Syntax;

namespace Helix.Parsing {
    public partial class Parser {
        private IParseSyntax MemberAccess(IParseSyntax first) {
            this.Advance(TokenKind.Dot);

            var tok = this.Advance(TokenKind.Identifier);
            var loc = first.Location.Span(tok.Location);

            return new MemberAccessParseSyntax {
                Location = loc,
                Operand = first,
                MemberName = tok.Value
            };
        }
        
        private IDeclaration StructDeclaration() {
            var start = this.Advance(TokenKind.StructKeyword);
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

            var last = this.Advance(TokenKind.CloseBrace);
            var loc = start.Location.Span(last.Location);

            var sig = new ParseStructSignature {
                Location = loc,
                Name = name,
                Members = mems
            };

            return new StructParseDeclaration {
                Location = loc,
                Signature = sig
            };
        }
    }
}