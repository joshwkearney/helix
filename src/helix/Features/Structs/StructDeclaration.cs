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
        private IDeclaration StructDeclaration() {
            var start = this.Advance(TokenKind.StructKeyword);
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

            return new StructDeclaration(loc, sig);
        }
    }
}

namespace Helix.Features.Aggregates {
    public record StructDeclaration : IDeclaration {
        private readonly StructParseSignature signature;

        public TokenLocation Location { get; }

        public StructDeclaration(TokenLocation loc, StructParseSignature sig) {
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
                new TypeSyntax(this.Location, new NominalType(path, NominalTypeKind.Struct)));
        }

        public void DeclareTypes(TypeFrame types) {
            var path = this.Location.Scope.Append(this.signature.Name);
            var sig = this.signature.ResolveNames(types);

            types.NominalSignatures = types.NominalSignatures.SetItem(path, sig);

            // Register this declaration with the code generator so 
            // types are constructed in order
            types.TypeDeclarations[sig] = writer => this.RealCodeGenerator(sig, writer);
        }

        public IDeclaration CheckTypes(TypeFrame types) {
            var path = this.Location.Scope.Append(this.signature.Name);
            var sig = this.signature.ResolveNames(types);
            var structType = new NominalType(path, NominalTypeKind.Struct);

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
            var name = writer.GetVariableName(path);

            var mems = signature.Members
                .Select(x => new CParameter() {
                    Type = writer.ConvertType(x.Type),
                    Name = x.Name
                })
                .ToArray();

            var prototype = new CAggregateDeclaration() {
                Name = name
            };

            var fullDeclaration = new CAggregateDeclaration() {
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