using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Aggregates;
using Helix.Parsing;
using Helix.Generation.Syntax;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree MemberAccess(ISyntaxTree first, BlockBuilder block) {
            this.Advance(TokenKind.Dot);

            var tok = this.Advance(TokenKind.Identifier);
            var loc = first.Location.Span(tok.Location);

            return new MemberAccessSyntax(loc, first, tok.Value);
        }
    }
}

namespace Helix.Features.Aggregates {
    public record MemberAccessSyntax : ISyntaxTree {
        private readonly ISyntaxTree target;
        private readonly string memberName;
        private readonly bool isTypeChecked;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

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

            // Handle getting the count of an array
            if (targetType is ArrayType array) {
                if (this.memberName == "count") {
                    var result = new MemberAccessSyntax(
                        this.Location,
                        target,
                        "count",
                        true);

                    types.ReturnTypes[result] = PrimitiveType.Int;
                    types.CapturedVariables[result] = Array.Empty<IdentifierPath>();
                    return result;
                }
            }

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
                        
                        if (field.Type.IsValueType(types)) {
                            types.CapturedVariables[result] = Array.Empty<IdentifierPath>();
                        }
                        else {
                            types.CapturedVariables[result] = types.CapturedVariables[target];
                        }

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

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            return new CMemberAccess() {
                Target = this.target.GenerateCode(types, writer),
                MemberName = this.memberName
            };
        }
    }
}
