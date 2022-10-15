using System.Collections.Immutable;
using System.Linq;
using Trophy;
using Trophy.Analysis;
using Trophy.Analysis.SyntaxTree;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Aggregates;
using Trophy.Parsing;
using Trophy.Parsing.ParseTree;

namespace Trophy.Parsing
{
    public partial class Parser {
        private IParseTree MemberAccess(IParseTree first) {
            this.Advance(TokenKind.Dot);

            var tok = this.Advance(TokenKind.Identifier);
            var loc = first.Location.Span(tok.Location);

            return new MemberAccessParseTree(loc, first, tok.Value);
        }
    }
}

namespace Trophy.Features.Aggregates
{
    public class MemberAccessParseTree : IParseTree {
        private readonly IParseTree target;
        private readonly string memberName;

        public TokenLocation Location { get; }

        public MemberAccessParseTree(TokenLocation location, IParseTree target, string memberName) {
            this.Location = location;
            this.target = target;
            this.memberName = memberName;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types, TypeContext context) {
            var target = this.target.ResolveTypes(scope, names, types);

            // If this is a named type it could be a struct or union
            if (target.ReturnType.AsNamedType().Select(x => x.FullName).TryGetValue(out var path)) {

                // If this is a struct or union we can access the fields
                if (names.TryGetName(path).TryGetValue(out var name)) {
                    if (name == NameTarget.Struct || name == NameTarget.Union) {

                        var sig = types.GetAggregate(path);
                        var fieldOpt = sig
                            .Members
                            .Where(x => x.MemberName == this.memberName)
                            .FirstOrNone();

                        // Make sure this field is present
                        if (fieldOpt.TryGetValue(out var field)) {
                            return new MemberAccessSyntax(
                                this.Location,
                                target,
                                this.memberName,
                                field.MemberType);
                        }
                    }
                }                
            }

            throw TypeCheckingErrors.MemberUndefined(this.Location, target.ReturnType, this.memberName);
        }
    }

    public class MemberAccessSyntax : ISyntaxTree {
        private readonly ISyntaxTree target;
        private readonly string memberName;

        public TokenLocation Location { get; }

        public TrophyType ReturnType { get; }

        public MemberAccessSyntax(TokenLocation location, ISyntaxTree target, string memberName, TrophyType retType) {
            this.Location = location;
            this.target = target;
            this.memberName = memberName;
            this.ReturnType = retType;
        }

        public CExpression GenerateCode(CWriter writer, CStatementWriter statWriter) {
            var target = this.target.GenerateCode(writer, statWriter);

            return CExpression.MemberAccess(target, this.memberName);
        }
    }
}
