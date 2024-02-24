using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Aggregates;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Analysis.Flow;
using System.Reflection;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;

namespace Helix.Parsing {
    public partial class Parser {
        private IParseTree MemberAccess(IParseTree first) {
            this.Advance(TokenKind.Dot);

            var tok = this.Advance(TokenKind.Identifier);
            var loc = first.Location.Span(tok.Location);

            return new MemberAccessSyntax(loc, first, tok.Value, default);
        }
    }
}

namespace Helix.Features.Aggregates {
    public record MemberAccessSyntax : IParseTree {
        private static int tempCounter = 0;

        private readonly IdentifierPath path;

        public IParseTree Target { get; }

        public string MemberName { get; }

        public TokenLocation Location { get; }

        public IEnumerable<IParseTree> Children => new[] { this.Target };

        public bool IsPure => this.Target.IsPure;

        public MemberAccessSyntax(TokenLocation location, IParseTree target, 
                                  string memberName, IdentifierPath scope) {
            this.Location = location;
            this.Target = target;
            this.MemberName = memberName;
            this.path = scope?.Append("$mem" + tempCounter++);
        }
    }

    public record MemberAccessLValue : IParseTree {
        private readonly IParseTree target;
        private readonly string memberName;
        private readonly HelixType memberType;

        public TokenLocation Location { get; }

        public IEnumerable<IParseTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public MemberAccessLValue(TokenLocation location, IParseTree target, 
                                  string memberName, HelixType memberType) {
            this.Location = location;
            this.target = target;
            this.memberName = memberName;
            this.memberType = memberType;
        }
    }
}
