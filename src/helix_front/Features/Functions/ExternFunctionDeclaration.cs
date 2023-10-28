using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.CSyntax;
using Helix.Features.Functions;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Syntax;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Features.Types;
using Helix.Analysis;

namespace Helix.Parsing {
    public partial class Parser {
        private IDeclaration ExternFunctionDeclaration() {
            var start = this.Advance(TokenKind.ExternKeyword);
            var sig = this.FunctionSignature();
            var end = this.Advance(TokenKind.Semicolon);
            var loc = start.Location.Span(end.Location);

            return new ExternFunctionParseDeclaration(loc, sig);
        }
    }
}

namespace Helix.Features.Functions {
    public record ExternFunctionParseDeclaration : IDeclaration {
        public TokenLocation Location { get; }

        public FunctionParseSignature Signature { get; }

        public ExternFunctionParseDeclaration(TokenLocation loc, FunctionParseSignature sig) {
            this.Location = loc;
            this.Signature = sig;
        }

        public void DeclareNames(TypeFrame names) {
            FunctionsHelper.CheckForDuplicateParameters(
                this.Location,
                this.Signature.Parameters.Select(x => x.Name));

            FunctionsHelper.DeclareName(this.Signature, names);
        }

        public void DeclareTypes(TypeFrame types) {
            var path = types.Scope.Append(this.Signature.Name);
            var sig = this.Signature.ResolveNames(types);
            var named = new NominalType(path, NominalTypeKind.Function);

            // Replace the temporary wrapper object with a full declaration
            types.Locals = types.Locals.SetItem(path, new LocalInfo(named));

            // Declare this function
            types.NominalSignatures.Add(path, sig);
        }

        public IDeclaration CheckTypes(TypeFrame types) {
            var path = types.Scope.Append(this.Signature.Name);
            var sig = new NominalType(path, NominalTypeKind.Function).AsFunction(types).GetValue();

            return new ExternFunctionDeclaration(this.Location, sig, path);
        }

        public void GenerateCode(TypeFrame types, ICWriter writer) => throw new InvalidOperationException();
    }

    public record ExternFunctionDeclaration : IDeclaration {
        public FunctionType Signature { get; }

        public TokenLocation Location { get; }

        public IdentifierPath Path { get; }

        public ExternFunctionDeclaration(TokenLocation loc, FunctionType sig, IdentifierPath path) {
            this.Location = loc;
            this.Signature = sig;
            this.Path = path;
        }

        public void DeclareNames(TypeFrame names) {
            throw new InvalidOperationException();
        }

        public void DeclareTypes(TypeFrame types) {
            throw new InvalidOperationException();
        }

        public IDeclaration CheckTypes(TypeFrame types) => this;

        public void GenerateCode(TypeFrame types, ICWriter writer) {
            var returnType = this.Signature.ReturnType == PrimitiveType.Void
                ? new CNamedType("void")
                : writer.ConvertType(this.Signature.ReturnType, types);

            var pars = this.Signature
                .Parameters
                .Select((x, i) => new CParameter() {
                    Type = writer.ConvertType(x.Type, types),
                    Name = writer.GetVariableName(this.Path.Append(x.Name))
                })
                .Prepend(new CParameter() {
                    Name = "_region",
                    Type = new CNamedType("int")
                })
                .ToArray();

            var funcName = writer.GetVariableName(this.Path);

            var forwardDecl = new CFunctionDeclaration() {
                ReturnType = returnType,
                Name = funcName,
                Parameters = pars
            };

            writer.WriteDeclaration2(forwardDecl);
        }
    }
}
