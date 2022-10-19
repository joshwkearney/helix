using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Features.Aggregates;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree MemberAccess(ISyntaxTree first) {
            this.Advance(TokenKind.Dot);

            var tok = this.Advance(TokenKind.Identifier);
            var loc = first.Location.Span(tok.Location);

            return new MemberAccessSyntax(loc, first, tok.Value);
        }
    }
}

namespace Trophy.Features.Aggregates {
    public record MemberAccessSyntax : ISyntaxTree {
        private readonly ISyntaxTree target;
        private readonly string memberName;
        private readonly bool isTypeChecked;

        public TokenLocation Location { get; }

        public MemberAccessSyntax(TokenLocation location, ISyntaxTree target, 
                                  string memberName, bool isTypeChecked = false) {
            this.Location = location;
            this.target = target;
            this.memberName = memberName;
            this.isTypeChecked = isTypeChecked;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            var target = this.target.CheckTypes(types).ToRValue(types);
            var targetType = types.ReturnTypes[target];

            // If this is a named type it could be a struct or union
            if (targetType is NamedType named) {

                // If this is a struct or union we can access the fields
                if (types.Aggregates.TryGetValue(named.Path, out var sig)) {
                    var fieldOpt = sig
                        .Members
                        .Where(x => x.Name == this.memberName)
                        .FirstOrNone();

                    // Make sure this field is present
                    if (fieldOpt.TryGetValue(out var field)) {
                        var result = new MemberAccessSyntax(
                            this.Location,
                            target,
                            this.memberName,
                            true);

                        types.ReturnTypes[result] = field.Type;

                        return result;
                    }
                    
                }               
            }

            throw TypeCheckingErrors.MemberUndefined(this.Location, targetType, this.memberName);
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            if (!this.isTypeChecked) {
                throw TypeCheckingErrors.RValueRequired(this.Location);
            }

            return this;
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            return new CMemberAccess() {
                Target = this.target.GenerateCode(writer),
                MemberName = this.memberName
            };
        }
    }
}
