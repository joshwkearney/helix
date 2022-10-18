using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Features.Aggregates;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntax MemberAccess(ISyntax first) {
            this.Advance(TokenKind.Dot);

            var tok = this.Advance(TokenKind.Identifier);
            var loc = first.Location.Span(tok.Location);

            return new MemberAccessSyntax(loc, first, tok.Value);
        }
    }
}

namespace Trophy.Features.Aggregates {
    public record MemberAccessSyntax : ISyntax {
        private readonly ISyntax target;
        private readonly string memberName;
        private readonly bool isTypeChecked;

        public TokenLocation Location { get; }

        public MemberAccessSyntax(TokenLocation location, ISyntax target, 
                                  string memberName, bool isTypeChecked = false) {
            this.Location = location;
            this.target = target;
            this.memberName = memberName;
            this.isTypeChecked = isTypeChecked;
        }

        public Option<TrophyType> TryInterpret(INamesRecorder names) {
            return Option.None;
        }

        public ISyntax CheckTypes(ITypesRecorder types) {
            var target = this.target.CheckTypes(types).ToRValue(types);
            var targetType = types.GetReturnType(this.target);

            // If this is a named type it could be a struct or union
            if (targetType is NamedType named) {

                // If this is a struct or union we can access the fields
                if (types.TryResolveName(named.Path).TryGetValue(out var name)) {
                    if (name == NameTarget.Aggregate) {

                        var sig = types.GetAggregate(named.Path);
                        var fieldOpt = sig
                            .Members
                            .Where(x => x.MemberName == this.memberName)
                            .FirstOrNone();

                        // Make sure this field is present
                        if (fieldOpt.TryGetValue(out var field)) {
                            var result = new MemberAccessSyntax(
                                this.Location,
                                target,
                                this.memberName,
                                true);

                            types.SetReturnType(result, field.MemberType);

                            return result;
                        }
                    }
                }                
            }

            throw TypeCheckingErrors.MemberUndefined(this.Location, targetType, this.memberName);
        }

        public ISyntax ToRValue(ITypesRecorder types) {
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
