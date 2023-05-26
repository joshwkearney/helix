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
        private IDeclaration UnionDeclaration() {
            var start = this.Advance(TokenKind.UnionKeyword);
            var name = this.Advance(TokenKind.Identifier).Value;
            var mems = new List<ParseStructMember>();

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
                mems.Add(new ParseStructMember(memLoc, memName.Value, memType, isWritable));
            }

            this.Advance(TokenKind.CloseBrace);
            var last = this.Advance(TokenKind.Semicolon);
            var loc = start.Location.Span(last.Location);
            var sig = new StructParseSignature(loc, name, mems);

            return new UnionParseDeclaration(loc, sig);
        }
    }
}

namespace Helix.Features.Aggregates {
    public record UnionParseDeclaration : IDeclaration {
        private readonly StructParseSignature signature;

        public TokenLocation Location { get; }

        public UnionParseDeclaration(TokenLocation loc, StructParseSignature sig) {
            this.Location = loc;
            this.signature = sig;
        }

        public void DeclareNames(TypeFrame types) {
            // Make sure this name isn't taken
            if (types.TryResolvePath(types.Scope, this.signature.Name, out _)) {
                throw TypeException.IdentifierDefined(this.Location, this.signature.Name);
            }

            var path = types.Scope.Append(this.signature.Name);
            var named = new TypeSyntax(this.Location, new NominalType(path, NominalTypeKind.Union));

            types.SyntaxValues = types.SyntaxValues.SetItem(path, named);
        }

        public void DeclareTypes(TypeFrame types) {
            var path = types.Scope.Append(this.signature.Name);
            var structSig = this.signature.ResolveNames(types);
            var unionSig = new UnionType(structSig.Members);

            types.NominalSignatures = types.NominalSignatures.SetItem(path, unionSig);
        }

        public IDeclaration CheckTypes(TypeFrame types) {
            var path = types.Scope.Append(this.signature.Name);
            var sig = this.signature.ResolveNames(types);
            var unionSig = new UnionType(sig.Members);
            var structType = new NominalType(path, NominalTypeKind.Union);

            var isRecursive = sig.Members
                .Select(x => x.Type)
                .Where(x => x.IsValueType(types))
                .SelectMany(x => x.GetContainedTypes(types))
                .Contains(structType);

            // Make sure this is not a recursive struct or union
            if (isRecursive) {
                throw TypeException.CircularValueObject(this.Location, structType);
            }

            return new UnionDeclaration(this.Location, unionSig, path);
        }
    }

    public record UnionDeclaration : IDeclaration {
        private readonly UnionType signature;
        private readonly IdentifierPath path;

        public TokenLocation Location { get; }

        public UnionDeclaration(TokenLocation loc, UnionType sig, IdentifierPath path) {
            this.Location = loc;
            this.signature = sig;
            this.path = path;
        }

        public void DeclareNames(TypeFrame types) { }

        public void DeclareTypes(TypeFrame types) { }

        public IDeclaration CheckTypes(TypeFrame types) => this;

        public void AnalyzeFlow(FlowFrame flow) { }

        public void GenerateCode(FlowFrame types, ICWriter writer) { 
            var structName = writer.GetVariableName(this.path);
            var unionName = "_$" + writer.GetVariableName(this.path);

            var unionPrototype = new CAggregateDeclaration() {
                Name = unionName,
                IsUnion = true
            };

            var unionDeclaration = new CAggregateDeclaration() {
                Name = unionName,
                IsUnion = true,
                Members = this.signature.Members
                    .Select(x => new CParameter() {
                        Type = writer.ConvertType(x.Type),
                        Name = x.Name
                    })
                    .ToArray(),
            };

            var structPrototype = new CAggregateDeclaration() {
                Name = structName
            };

            var structDeclaration = new CAggregateDeclaration() {
                Name = structName,
                Members = new[] {
                    new CParameter() {
                        Name = "tag",
                        Type = new CNamedType("int")
                    },
                    new CParameter() {
                        Name = "data",
                        Type = new CNamedType(unionName)
                    }
                }
            };

            // Write forward declaration
            writer.WriteDeclaration1(unionPrototype);
            writer.WriteDeclaration1(structPrototype);
            writer.WriteDeclaration3(new CEmptyLine());

            // Write full struct
            writer.WriteDeclaration3(unionDeclaration);
            writer.WriteDeclaration3(new CEmptyLine());

            writer.WriteDeclaration3(structDeclaration);
            writer.WriteDeclaration3(new CEmptyLine());
        }
    }
}