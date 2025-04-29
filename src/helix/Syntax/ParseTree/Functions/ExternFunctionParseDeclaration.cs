using Helix.CodeGeneration;
using Helix.Parsing;
using Helix.Syntax.TypedTree.Functions;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Functions;

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

        return types.WithSignature(path, sig);
    }

    public TypeCheckResult<IDeclaration> CheckTypes(TypeFrame types) {
        var path = types.Scope.Append(this.Signature.Name);
        var sig = new NominalType(path, NominalTypeKind.Function).AsFunction(types).GetValue();

        var result = new ExternFunctionDeclaration {
            Location = this.Location,
            Signature = sig,
            Path = path
        };

        return new(result, types);
    }

    public void GenerateCode(TypeFrame types, ICWriter writer) => throw new InvalidOperationException();
}