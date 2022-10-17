using Trophy.Analysis;
using Trophy.Generation;
using Trophy.Generation.CSyntax;
using Trophy.Features.Aggregates;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

namespace Trophy.Parsing {
    public partial class Parser {
        private IDeclarationTree AggregateDeclaration() {
            Token start;
            if (this.Peek(TokenKind.StructKeyword)) {
                start = this.Advance(TokenKind.StructKeyword);
            }
            else {
                start = this.Advance(TokenKind.UnionKeyword);
            }

            var name = this.Advance(TokenKind.Identifier).Value;
            var mems = new List<ParseAggregateMember>();

            this.Advance(TokenKind.OpenBrace);

            while (!this.Peek(TokenKind.CloseBrace)) {
                bool isWritable;
                Token memStart;

                if (this.Peek(TokenKind.VarKeyword)) {
                    memStart = this.Advance(TokenKind.VarKeyword);
                    isWritable = true;
                }
                else {
                    memStart = this.Advance(TokenKind.LetKeyword);
                    isWritable = false;
                }

                var memName = this.Advance(TokenKind.Identifier);
                this.Advance(TokenKind.AsKeyword);

                var memType = this.TopExpression();
                var memLoc = memStart.Location.Span(memType.Location);

                this.Advance(TokenKind.Semicolon);
                mems.Add(new ParseAggregateMember(memLoc, memName.Value, memType, isWritable));
            }

            this.Advance(TokenKind.CloseBrace);
            var last = this.Advance(TokenKind.Semicolon);
            var loc = start.Location.Span(last.Location);
            var sig = new AggregateParseSignature(name, mems);
            var kind = start.Kind == TokenKind.StructKeyword ? AggregateKind.Struct : AggregateKind.Union;

            return new AggregateParseDeclaration(loc, sig, kind);
        }
    }
}

namespace Trophy.Features.Aggregates {
    public enum AggregateKind {
        Struct, Union
    }

    public record AggregateParseDeclaration : IDeclarationTree {
        private readonly AggregateParseSignature signature;
        private readonly AggregateKind kind;

        public TokenLocation Location { get; }

        public AggregateParseDeclaration(TokenLocation loc, AggregateParseSignature sig, AggregateKind kind) {
            this.Location = loc;
            this.signature = sig;
            this.kind = kind;
        }

        public void DeclareNames(INamesRecorder names) {
            // Make sure this name isn't taken
            if (!names.DeclareName(this.signature.Name, NameTarget.Aggregate)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.signature.Name);
            }

            names = names.WithScope(this.signature.Name);

            // Declare the parameters
            foreach (var par in this.signature.Members) {
                if (!names.DeclareName(par.MemberName, NameTarget.Reserved)) {
                    throw TypeCheckingErrors.IdentifierDefined(this.Location, par.MemberName);
                }
            }
        }

        public void DeclareTypes(ITypesRecorder types) {
            var sig = this.signature.ResolveNames(types);
            types.DeclareAggregate(sig);

            foreach (var mem in this.signature.Members) {
                types.DeclareReserved(sig.Path.Append(mem.MemberName));
            }
        }

        public IDeclarationTree CheckTypes(ITypesRecorder types) {
            var path = types.TryFindPath(this.signature.Name).GetValue();
            var sig = types.GetAggregate(path);

            return new AggregateDeclaration(this.Location, sig, this.kind);
        }

        public void GenerateCode(ICWriter writer) => throw new InvalidOperationException();
    }

    public record AggregateDeclaration : IDeclarationTree {
        private readonly AggregateSignature signature;
        private readonly AggregateKind kind;

        public TokenLocation Location { get; }

        public AggregateDeclaration(TokenLocation loc, AggregateSignature sig, AggregateKind kind) {
            this.Location = loc;
            this.signature = sig;
            this.kind = kind;
        }

        public void DeclareNames(INamesRecorder names) { }

        public void DeclareTypes(ITypesRecorder types) { }

        public IDeclarationTree CheckTypes(ITypesRecorder types) => this;

        public void GenerateCode(ICWriter writer) {
            var name = writer.GetVariableName(this.signature.Path);

            var mems = this.signature.Members
                .Select(x => new CParameter() { 
                    Type = writer.ConvertType(x.MemberType),
                    Name = writer.GetVariableName(this.signature.Path.Append(x.MemberName))
                })
                .ToArray();

            var prototype = new CAggregateDeclaration() {
                Kind = this.kind,
                Name = name
            };

            var fullDeclaration = new CAggregateDeclaration() {
                Kind = this.kind,
                Name = name,
                Members = mems
            };

            // Write forward declaration
            writer.WriteDeclaration1(prototype);
            writer.WriteDeclaration1(new CEmptyLine());

            // Write full struct
            writer.WriteDeclaration2(fullDeclaration);
            writer.WriteDeclaration2(new CEmptyLine());
        }
    }
}