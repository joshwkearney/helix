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
            var syntax = new TypeSyntax(this.Location, new NominalType(path, NominalTypeKind.Struct));

            types.SyntaxValues = types.SyntaxValues.SetItem(path, syntax);
        }

        public void DeclareTypes(TypeFrame types) {
            var path = types.Scope.Append(this.signature.Name);
            var sig = this.signature.ResolveNames(types);

            types.NominalSignatures.Add(path, sig);
        }

        public IDeclaration CheckTypes(TypeFrame types) {
            var path = types.Scope.Append(this.signature.Name);
            var named = new NominalType(path, NominalTypeKind.Struct);
            var sig = named.AsStruct(types).GetValue();

            var isRecursive = sig.Members
                .Select(x => x.Type)
                .Where(x => x.IsValueType(types))
                .SelectMany(x => x.GetContainedTypes(types))
                .Contains(named);

            // Make sure this is not a recursive struct or union
            if (isRecursive) {
                throw TypeException.CircularValueObject(this.Location, named);
            }

            return new StructDeclaration(this.Location, sig, path);
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

        public IDeclaration CheckTypes(TypeFrame types) => this;

        public void AnalyzeFlow(FlowFrame flow) { }

        public void GenerateCode(FlowFrame types, ICWriter writer) {
            var name = writer.GetVariableName(this.path);

            var mems = this.signature.Members
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