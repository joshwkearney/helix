using Trophy.Analysis;
using Trophy.Generation;
using Trophy.Generation.CSyntax;
using Trophy.Features.Aggregates;
using Trophy.Parsing;
using Trophy.Generation.Syntax;
using Trophy.Analysis.Types;

namespace Trophy.Parsing {
    public partial class Parser {
        private IDeclaration AggregateDeclaration() {
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
            var kind = start.Kind == TokenKind.StructKeyword ? AggregateKind.Struct : AggregateKind.Union;
            var sig = new AggregateParseSignature(name, kind, mems);

            return new AggregateParseDeclaration(loc, sig, kind);
        }
    }
}

namespace Trophy.Features.Aggregates {
    public enum AggregateKind {
        Struct, Union
    }

    public record AggregateParseDeclaration : IDeclaration {
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

        public IDeclaration CheckTypes(ITypesRecorder types) {
            var path = types.TryFindPath(this.signature.Name).GetValue();
            var sig = types.GetAggregate(path);

            var structType = new NamedType(path);
            var isRecursive = sig.Members
                .SelectMany(x => x.MemberType.GetContainedValueTypes(types))
                .Contains(structType);

            // Make sure this is not a recursive struct or union
            if (isRecursive) {
                throw TypeCheckingErrors.CircularValueObject(this.Location, structType);
            }

            return new AggregateDeclaration(this.Location, sig, this.kind);
        }

        public void GenerateCode(ICWriter writer) => throw new InvalidOperationException();
    }

    public record AggregateDeclaration : IDeclaration {
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

        public IDeclaration CheckTypes(ITypesRecorder types) => this;

        public void GenerateCode(ICWriter writer) {
            var name = writer.GetVariableName(this.signature.Path);

            var mems = this.signature.Members
                .Select(x => new CParameter() { 
                    Type = writer.ConvertType(x.MemberType),
                    Name = x.MemberName
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