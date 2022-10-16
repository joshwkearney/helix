using Trophy.Analysis;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Aggregates;
using Trophy.Parsing;

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

    public class AggregateParseDeclaration : IDeclarationTree {
        private readonly AggregateParseSignature signature;
        private readonly AggregateKind kind;

        public TokenLocation Location { get; }

        public AggregateParseDeclaration(TokenLocation loc, AggregateParseSignature sig, AggregateKind kind) {
            this.Location = loc;
            this.signature = sig;
            this.kind = kind;
        }

        public void DeclareNames(IdentifierPath scope, TypesRecorder types) {
            bool nameTaken;

            // Declare this struct
            if (this.kind == AggregateKind.Struct) {
                nameTaken = !types.TrySetNameTarget(scope, this.signature.Name, NameTarget.Struct);
            }
            else {
                nameTaken = !types.TrySetNameTarget(scope, this.signature.Name, NameTarget.Union);
            }

            if (nameTaken) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.signature.Name);
            }

            // Declare the parameters
            foreach (var par in this.signature.Members) {
                if (!types.TrySetNameTarget(scope.Append(this.signature.Name), par.MemberName, NameTarget.Reserved)) {
                    throw TypeCheckingErrors.IdentifierDefined(this.Location, par.MemberName);
                }
            }
        }

        public void DeclareTypes(IdentifierPath scope, TypesRecorder types) {
            var sig = this.signature.ResolveNames(scope, types);

            // Declare this aggregate
            types.SetAggregate(sig);
        }

        public IDeclarationTree ResolveTypes(IdentifierPath scope, TypesRecorder types) {
            var sig = this.signature.ResolveNames(scope, types);

            return new AggregateDeclaration(this.Location, sig, this.kind);
        }

        public void GenerateCode(TypesRecorder types, CWriter writer) {
            throw new InvalidOperationException();
        }
    }

    public class AggregateDeclaration : IDeclarationTree {
        private readonly AggregateSignature signature;
        private readonly AggregateKind kind;

        public TokenLocation Location { get; }

        public AggregateDeclaration(TokenLocation loc, AggregateSignature sig, AggregateKind kind) {
            this.Location = loc;
            this.signature = sig;
            this.kind = kind;
        }

        public void DeclareNames(IdentifierPath scope, TypesRecorder types) { }

        public void DeclareTypes(IdentifierPath scope, TypesRecorder types) { }

        public IDeclarationTree ResolveTypes(IdentifierPath scope, TypesRecorder types) => this;

        public void GenerateCode(TypesRecorder types, CWriter writer) {
            var name = writer.GetVariableName(this.signature.Path);

            if (this.kind == AggregateKind.Struct) {
                // Write forward declaration
                writer.WriteDeclaration1(CDeclaration.StructPrototype(name));
                writer.WriteDeclaration1(CDeclaration.EmptyLine());

                // Write full struct
                writer.WriteDeclaration2(CDeclaration.Struct(
                    name,
                    this.signature.Members
                        .Select(x => new CParameter(
                            writer.ConvertType(x.MemberType), 
                            writer.GetVariableName(this.signature.Path.Append(x.MemberName))))
                        .ToArray()));

                writer.WriteDeclaration2(CDeclaration.EmptyLine());
            }
            else if (this.kind == AggregateKind.Union) {
                // Write forward declaration
                writer.WriteDeclaration1(CDeclaration.UnionPrototype(name));
                writer.WriteDeclaration1(CDeclaration.EmptyLine());

                // Write full union
                writer.WriteDeclaration2(CDeclaration.Union(
                    name,
                    this.signature.Members
                        .Select(x => new CParameter(writer.ConvertType(x.MemberType), x.MemberName))
                        .ToArray()));

                writer.WriteDeclaration2(CDeclaration.EmptyLine());
            }
            else {
                throw new Exception("Unexpected aggregate kind");
            }
        }
    }
}