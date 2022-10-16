using Trophy.Analysis;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Aggregates;
using Trophy.Parsing;

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
    public class MemberAccessSyntax : ISyntaxTree {
        private readonly ISyntaxTree target;
        private readonly string memberName;

        public TokenLocation Location { get; }

        public MemberAccessSyntax(TokenLocation location, ISyntaxTree target, string memberName) {
            this.Location = location;
            this.target = target;
            this.memberName = memberName;
        }

        public Option<TrophyType> ToType(IdentifierPath scope, TypesRecorder types) {
            return Option.None;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, TypesRecorder types) {
            if (this.target.ResolveTypes(scope, types).ToRValue(types).TryGetValue(out var target)) {
                throw TypeCheckingErrors.RValueRequired(this.target.Location);
            }

            var targetType = types.GetReturnType(this.target);

            // If this is a named type it could be a struct or union
            if (targetType.AsNamedType().Select(x => x.FullName).TryGetValue(out var path)) {

                // If this is a struct or union we can access the fields
                if (types.TryGetNameTarget(path).TryGetValue(out var name)) {
                    if (name == NameTarget.Struct || name == NameTarget.Union) {

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
                                this.memberName);

                            types.SetReturnType(result, field.MemberType);

                            return result;
                        }
                    }
                }                
            }

            throw TypeCheckingErrors.MemberUndefined(this.Location, targetType, this.memberName);
        }

        public Option<ISyntaxTree> ToRValue(TypesRecorder types) => this;

        public Option<ISyntaxTree> ToLValue(TypesRecorder types) => Option.None;

        public CExpression GenerateCode(TypesRecorder types, CStatementWriter writer) {
            var target = this.target.GenerateCode(types, writer);

            return CExpression.MemberAccess(target, this.memberName);
        }
    }
}
