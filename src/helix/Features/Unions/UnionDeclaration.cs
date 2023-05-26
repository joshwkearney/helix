using Helix.Generation;
using Helix.Generation.CSyntax;
using Helix.Features.Aggregates;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Analysis.Types;
using Helix.Syntax;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;

namespace Helix.Parsing {
    public partial class Parser {
        private IDeclaration UnionDeclaration() {
            var start = this.Advance(TokenKind.UnionKeyword);
            var name = this.Advance(TokenKind.Identifier).Value;
            var mems = new List<ParseStructMember>();

            this.Advance(TokenKind.OpenBrace);

            while (!this.Peek(TokenKind.CloseBrace)) {
                var memStart = this.Advance(TokenKind.LetKeyword);
                var memName = this.Advance(TokenKind.Identifier);
                this.Advance(TokenKind.AsKeyword);

                var memType = this.TopExpression();
                var memLoc = memStart.Location.Span(memType.Location);

                this.Advance(TokenKind.Semicolon);
                mems.Add(new ParseStructMember(memLoc, memName.Value, memType, false));
            }

            this.Advance(TokenKind.CloseBrace);
            var last = this.Advance(TokenKind.Semicolon);
            var loc = start.Location.Span(last.Location);
            var sig = new StructParseSignature(loc, name, mems);

            return new UnionDeclaration(loc, sig);
        }
    }
}

namespace Helix.Features.Aggregates {
    public record UnionDeclaration : IDeclaration {
        private readonly StructParseSignature signature;

        public TokenLocation Location { get; }

        public UnionDeclaration(TokenLocation loc, StructParseSignature sig) {
            this.Location = loc;
            this.signature = sig;
        }

        public void DeclareNames(TypeFrame names) {
            // Make sure this name isn't taken
            if (names.TryResolvePath(this.Location.Scope, this.signature.Name, out _)) {
                throw TypeException.IdentifierDefined(this.Location, this.signature.Name);
            }

            var path = this.Location.Scope.Append(this.signature.Name);

            names.SyntaxValues = names.SyntaxValues.SetItem(
                path, 
                new TypeSyntax(this.Location, new NominalType(path, NominalTypeKind.Union)));
        }

        public void DeclareTypes(TypeFrame types) {
            var path = this.Location.Scope.Append(this.signature.Name);
            var sig = this.signature.ResolveNames(types);
            var unionType = new NominalType(path, NominalTypeKind.Union);

            types.Unions[path] = sig;

            // Register this declaration with the code generator so 
            // types are constructed in order
            types.TypeDeclarations[unionType] = writer => this.RealCodeGenerator(sig, writer);
        }

        public IDeclaration CheckTypes(TypeFrame types) {
            var path = this.Location.Scope.Append(this.signature.Name);
            var sig = this.signature.ResolveNames(types);
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

            return this;
        }

        public void AnalyzeFlow(FlowFrame flow) { }

        public void GenerateCode(FlowFrame types, ICWriter writer) { }

        private void RealCodeGenerator(StructType signature, ICWriter writer) {
            var path = this.Location.Scope.Append(this.signature.Name);
            var structName = writer.GetVariableName(path);
            var unionName = writer.GetVariableName(path) + "$union";

            var unionMems = signature.Members
                .Select(x => new CParameter() {
                    Type = writer.ConvertType(x.Type),
                    Name = x.Name
                })
                .ToArray();

            var unionPrototype = new CAggregateDeclaration() {
                Name = unionName,
                IsUnion = true
            };

            var unionDeclaration = new CAggregateDeclaration() {
                Name = unionName,
                Members = unionMems,
                IsUnion = true
            };

            var structMems = new[] { 
                new CParameter() {
                    Name = "tag",
                    Type = new CNamedType("int")
                },
                new CParameter() {
                    Name = "data",
                    Type = new CNamedType(unionName)
                }
            };

            var structPrototype = new CAggregateDeclaration() {
                Name = structName
            };

            var structDeclaration = new CAggregateDeclaration() {
                Name = structName,
                Members = structMems
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