using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Functions;

public record ExternFunctionParseDeclaration : IDeclaration {
    public required TokenLocation Location { get; init; }

    public required ParseFunctionSignature Signature { get; init; }

    public TypeFrame DeclareNames(TypeFrame names) {
        FunctionsHelper.CheckForDuplicateParameters(
            this.Location,
            this.Signature.Parameters.Select(x => x.Name));

        return FunctionsHelper.DeclareName(this.Signature, names);
    }

    public TypeFrame DeclareTypes(TypeFrame types) {
        var path = types.Scope.Append(this.Signature.Name);
        var sig = this.Signature.ResolveNames(types);

        return types.WithDeclaration(path, DeclarationKind.Function, sig);
    }

    public IDeclaration CheckTypes(TypeFrame types) {
        var path = types.Scope.Append(this.Signature.Name);
        var sig = new NominalType(path, NominalTypeKind.Function).AsFunction(types).GetValue();

        return new ExternFunctionDeclaration {
            Location = this.Location,
            Signature = sig,
            Path = path
        };
    }

    public void GenerateCode(TypeFrame types, ICWriter writer) => throw new InvalidOperationException();
}