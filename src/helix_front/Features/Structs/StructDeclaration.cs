using Helix.Generation;
using Helix.Generation.CSyntax;
using Helix.Features.Aggregates;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Analysis.Types;
using Helix.Syntax;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Analysis;

namespace Helix.Parsing {
    public partial class Parser {
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
                mems.Add(new ParseStructMember(memLoc, memName.Value, memType));
            }

            this.Advance(TokenKind.CloseBrace);
            var last = this.Advance(TokenKind.Semicolon);
            var loc = start.Location.Span(last.Location);
            var sig = new StructParseSignature(loc, name, mems);

            return new StructParseDeclaration(loc, sig);
        }
    }
}

namespace Helix.Features.Aggregates {
    public record StructParseDeclaration : IDeclaration {
        private readonly StructParseSignature signature;

        public TokenLocation Location { get; }

        public StructParseDeclaration(TokenLocation loc, StructParseSignature sig) {
            this.Location = loc;
            this.signature = sig;
        }

        public void DeclareNames(TypeFrame types) {
            // Make sure this name isn't taken
            if (types.TryResolvePath(types.Scope, this.signature.Name, out _)) {
                throw TypeException.IdentifierDefined(this.Location, this.signature.Name);
            }

            var path = types.Scope.Append(this.signature.Name);
            var named = new NominalType(path, NominalTypeKind.Struct);

            types.Locals = types.Locals.SetItem(path, new LocalInfo(named));
        }

        public void DeclareTypes(TypeFrame types) {
            var path = types.Scope.Append(this.signature.Name);
            var sig = this.signature.ResolveNames(types);

            types.Locals = types.Locals.Add(path, new LocalInfo(sig));
        }
    }

    public record StructDeclaration : IDeclaration {
        private readonly StructType signature;
        private readonly IdentifierPath path;

        public TokenLocation Location { get; }

        public StructDeclaration(TokenLocation loc, StructType sig, IdentifierPath path) {
            this.Location = loc;
            this.signature = sig;
            this.path = path;
        }

        public void DeclareNames(TypeFrame types) { }

        public void DeclareTypes(TypeFrame types) { }
    }
}