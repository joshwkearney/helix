using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Generation.CSyntax;
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

        public Option<TrophyType> ToType(INamesRecorder names) {
            return Option.None;
        }

        public ISyntaxTree CheckTypes(ITypesRecorder types) {
            if (this.target.CheckTypes(types).ToRValue(types).TryGetValue(out var target)) {
                throw TypeCheckingErrors.RValueRequired(this.target.Location);
            }

            var targetType = types.GetReturnType(this.target);

            // If this is a named type it could be a struct or union
            if (targetType.AsNamedType().Select(x => x.FullName).TryGetValue(out var path)) {

                // If this is a struct or union we can access the fields
                if (types.TryResolveName(path).TryGetValue(out var name)) {
                    if (name == NameTarget.Aggregate) {

                        var sig = types.GetAggregate(path);
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

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) {
            return this.isTypeChecked ? this : Option.None;
        }

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) => Option.None;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            return new CMemberAccess() {
                Target = this.target.GenerateCode(writer),
                MemberName = this.memberName
            };
        }
    }
}
