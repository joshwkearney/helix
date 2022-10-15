using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Analysis.SyntaxTree;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Aggregates;
using Trophy.Parsing;
using Trophy.Parsing.ParseTree;
using static System.Formats.Asn1.AsnWriter;

namespace Trophy.Parsing {
    public partial class Parser {
        private IParseDeclaration AggregateDeclaration() {
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
                var memName = this.Advance(TokenKind.Identifier).Value;
                this.Advance(TokenKind.AsKeyword);

                var memType = this.TypeExpression();

                this.Advance(TokenKind.Semicolon);
                mems.Add(new ParseAggregateMember(memName, memType));
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

    public class AggregateParseDeclaration : IParseDeclaration {
        public TokenLocation Location { get; }

        public AggregateParseSignature Signature { get; }

        public AggregateKind Kind { get; }

        public AggregateParseDeclaration(TokenLocation loc, AggregateParseSignature sig, AggregateKind kind) {
            this.Location = loc;
            this.Signature = sig;
            this.Kind = kind;
        }

        public void DeclareNames(IdentifierPath scope, NamesRecorder names) {
            // Declare this struct
            if (this.Kind == AggregateKind.Struct) {
                names.PutName(scope, this.Signature.Name, NameTarget.Struct);
            }
            else {
                names.PutName(scope, this.Signature.Name, NameTarget.Union);
            }

            // Declare the parameters
            foreach (var par in this.Signature.Members) {
                names.PutName(scope.Append(this.Signature.Name), par.MemberName, NameTarget.Reserved);
            }
        }

        public void DeclareTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types) {
            var sig = this.Signature.ResolveNames(scope, names);

            // Declare this aggregate
            types.Aggregates[sig.Path] = sig;
        }

        public IDeclaration ResolveTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types) {
            var sig = this.Signature.ResolveNames(scope, names);

            return new AggregateDeclaration(this.Location, sig, this.Kind);
        }
    }

    public class AggregateDeclaration : IDeclaration {
        public TokenLocation Location { get; }

        public AggregateSignature Signature { get; }

        public AggregateKind Kind { get; }

        public AggregateDeclaration(TokenLocation loc, AggregateSignature sig, AggregateKind kind) {
            this.Location = loc;
            this.Signature = sig;
            this.Kind = kind;
        }

        public void GenerateCode(CWriter writer) {
            var name = this.Signature.Path.ToCName();

            if (this.Kind == AggregateKind.Struct) {
                // Write forward declaration
                writer.WriteDeclaration1(CDeclaration.StructPrototype(name));
                writer.WriteDeclaration1(CDeclaration.EmptyLine());

                // Write full struct
                writer.WriteDeclaration2(CDeclaration.Struct(
                    name,
                    this.Signature.Members
                        .Select(x => new CParameter(writer.ConvertType(x.MemberType), x.MemberName))
                        .ToArray()));

                writer.WriteDeclaration2(CDeclaration.EmptyLine());
            }
            else if (this.Kind == AggregateKind.Union) {
                // Write forward declaration
                writer.WriteDeclaration1(CDeclaration.UnionPrototype(name));
                writer.WriteDeclaration1(CDeclaration.EmptyLine());

                // Write full union
                writer.WriteDeclaration2(CDeclaration.Union(
                    name,
                    this.Signature.Members
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