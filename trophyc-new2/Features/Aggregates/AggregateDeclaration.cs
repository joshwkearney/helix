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

            return new AggregateDeclaration(loc, sig, kind);
        }
    }
}

namespace Trophy.Features.Aggregates {
    public enum AggregateKind {
        Struct, Union
    }

    public class AggregateDeclaration : IDeclarationTree {
        private readonly AggregateParseSignature signature;
        private readonly AggregateKind kind;

        public TokenLocation Location { get; }

        public AggregateDeclaration(TokenLocation loc, AggregateParseSignature sig, AggregateKind kind) {
            this.Location = loc;
            this.signature = sig;
            this.kind = kind;
        }

        public void DeclareNames(INamesRecorder names) {
            // Make sure this name isn't taken
            if (!names.DeclareName(this.signature.Name, NameTarget.Aggregate)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.signature.Name);
            }

            names.PushScope(this.signature.Name);

            // Declare the parameters
            foreach (var par in this.signature.Members) {
                if (!names.DeclareName(par.MemberName, NameTarget.Reserved)) {
                    throw TypeCheckingErrors.IdentifierDefined(this.Location, par.MemberName);
                }
            }

            names.PopScope();
        }

        public void DeclarePaths(ITypesRecorder types) {
            var sig = this.signature.ResolveNames(types);
            types.DeclareAggregate(sig);

            types.PushScope(this.signature.Name);

            foreach (var mem in this.signature.Members) {
                types.DeclareReserved(sig.Path.Append(mem.MemberName));
            }

            types.PopScope();
        }

        public IDeclarationTree CheckTypes(ITypesRecorder types) => this;

        public void GenerateCode(CWriter writer) {
            var path = writer.TryFindPath(this.signature.Name).GetValue();
            var sig = writer.GetAggregate(path);
            var name = writer.GetVariableName(path);

            if (this.kind == AggregateKind.Struct) {
                // Write forward declaration
                writer.WriteDeclaration1(CDeclaration.StructPrototype(name));
                writer.WriteDeclaration1(CDeclaration.EmptyLine());

                // Write full struct
                writer.WriteDeclaration2(CDeclaration.Struct(
                    name,
                    sig.Members
                        .Select(x => new CParameter(
                            writer.ConvertType(x.MemberType),
                            writer.GetVariableName(path.Append(x.MemberName))))
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
                    sig.Members
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