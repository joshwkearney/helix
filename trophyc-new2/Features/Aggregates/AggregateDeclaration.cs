using Trophy.Analysis;
using Trophy.Generation;
using Trophy.Generation.CSyntax;
using Trophy.Features.Aggregates;
using Trophy.Parsing;
using Trophy.Generation.Syntax;
using Trophy.Analysis.Types;
using System.IO;

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

                var memType = this.TopExpression(new BlockBuilder());
                var memLoc = memStart.Location.Span(memType.Location);

                this.Advance(TokenKind.Semicolon);
                mems.Add(new ParseAggregateMember(memLoc, memName.Value, memType, isWritable));
            }

            this.Advance(TokenKind.CloseBrace);
            var last = this.Advance(TokenKind.Semicolon);
            var loc = start.Location.Span(last.Location);
            var kind = start.Kind == TokenKind.StructKeyword ? AggregateKind.Struct : AggregateKind.Union;
            var sig = new AggregateParseSignature(loc, name, kind, mems);

            return new AggregateDeclaration(loc, sig, kind);
        }
    }
}

namespace Trophy.Features.Aggregates {
    public enum AggregateKind {
        Struct, Union
    }

    public record AggregateDeclaration : IDeclaration {
        private readonly AggregateParseSignature signature;
        private readonly AggregateKind kind;

        public TokenLocation Location { get; }

        public AggregateDeclaration(TokenLocation loc, AggregateParseSignature sig, AggregateKind kind) {
            this.Location = loc;
            this.signature = sig;
            this.kind = kind;
        }

        public void DeclareNames(SyntaxFrame names) {
            // Make sure this name isn't taken
            if (names.TryResolvePath(this.Location.Scope, this.signature.Name, out _)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.signature.Name);
            }

            var path = this.Location.Scope.Append(this.signature.Name);

            names.Trees[path] = new TypeSyntax(this.Location, new NamedType(path));
            //names = names.WithScope(this.signature.Name);

            // Declare the parameters
            //foreach (var par in this.signature.Members) {
            //    if (!names.DeclareName(par.MemberName, NameTarget.Reserved)) {
            //        throw TypeCheckingErrors.IdentifierDefined(this.Location, par.MemberName);
            //    }
            //}
        }

        public void DeclareTypes(SyntaxFrame types) {
            var sig = this.signature.ResolveNames(types);

            types.Aggregates[sig.Path] = sig;

            //foreach (var mem in this.signature.Members) {
            //    types.DeclareReserved(sig.Path.Append(mem.MemberName));
            //}
        }

        public IDeclaration CheckTypes(SyntaxFrame types) {
            var sig = this.signature.ResolveNames(types); 
            var structType = new NamedType(sig.Path);
            var isRecursive = sig.Members
                .SelectMany(x => x.Type.GetContainedValueTypes(types))
                .Contains(structType);

            // Make sure this is not a recursive struct or union
            if (isRecursive) {
                throw TypeCheckingErrors.CircularValueObject(this.Location, structType);
            }

            // Disallow pointers in unions
            if (sig.Kind == AggregateKind.Union) {
                // For loop for better error messages
                for (int i = 0; i < sig.Members.Count; i++) {
                    var pointer = sig.Members[i].Type
                        .GetContainedValueTypes(types)
                        .Where(x => x is PointerType)
                        .FirstOrNone();

                    if (pointer.HasValue) {
                        throw new TypeCheckingException(
                            this.signature.Members[i].Location,
                            "Invalid Union Type",
                            $"The pointer type '{pointer.GetValue()}' cannot be a union member.");
                    }
                }
            }

            var type = new NamedType(sig.Path);

            // Register this declaration with the code generator so 
            // types are constructed in order
            types.TypeDeclarations[type] = writer => this.RealCodeGenerator(sig, writer);

            return this;
        }

        public void GenerateCode(ICWriter writer) { }

        private void RealCodeGenerator(AggregateSignature signature, ICWriter writer) {
            var name = writer.GetVariableName(signature.Path);

            var mems = signature.Members
                .Select(x => new CParameter() {
                    Type = writer.ConvertType(x.Type),
                    Name = x.Name
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

            // Write full struct
            writer.WriteDeclaration3(fullDeclaration);
            writer.WriteDeclaration3(new CEmptyLine());
        }
    }
}