using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.Structs;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Unions;

public record UnionParseDeclaration : IDeclaration {
    private readonly ParseStructSignature signature;

    public TokenLocation Location { get; }

    public UnionParseDeclaration(TokenLocation loc, ParseStructSignature sig) {
        this.Location = loc;
        this.signature = sig;
    }

    public TypeFrame DeclareNames(TypeFrame types) {
        // Make sure this name isn't taken
        if (types.TryResolvePath(types.Scope, this.signature.Name, out _)) {
            throw TypeException.IdentifierDefined(this.Location, this.signature.Name);
        }

        var path = types.Scope.Append(this.signature.Name);
        var named = new NominalType(path, NominalTypeKind.Union);

        return types.WithDeclaration(path, DeclarationKind.Type, named);
    }

    public TypeFrame DeclareTypes(TypeFrame types) {
        var path = types.Scope.Append(this.signature.Name);
        var structSig = this.signature.ResolveNames(types);
        var unionSig = new UnionType(structSig.Members);

        return types.WithNominalSignature(path, unionSig);
    }

    public DeclarationTypeCheckResult CheckTypes(TypeFrame types) {
        var path = types.Scope.Append(this.signature.Name);
        var sig = this.signature.ResolveNames(types);
        var unionSig = new UnionType(sig.Members);
        var structType = new NominalType(path, NominalTypeKind.Union);

        var isRecursive = sig.Members
            .Select(x => x.Type)
            .Where(x => x.IsValueType(types))
            .SelectMany(x => x.GetAccessibleTypes(types))
            .Contains(structType);

        // Make sure this is not a recursive struct or union
        if (isRecursive) {
            throw TypeException.CircularValueObject(this.Location, structType);
        }

        var result = new UnionDeclaration(this.Location, unionSig, path);

        return new(result, types);
    }
}