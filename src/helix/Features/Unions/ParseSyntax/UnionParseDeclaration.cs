using Helix.Analysis;
using Helix.Analysis.Flow;
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

    public void DeclareNames(TypeFrame types) {
        // Make sure this name isn't taken
        if (types.TryResolvePath(types.Scope, this.signature.Name, out _)) {
            throw TypeException.IdentifierDefined(this.Location, this.signature.Name);
        }

        var path = types.Scope.Append(this.signature.Name);
        var named = new NominalType(path, NominalTypeKind.Union);

        types.Locals = types.Locals.SetItem(path, new LocalInfo(named));
    }

    public void DeclareTypes(TypeFrame types) {
        var path = types.Scope.Append(this.signature.Name);
        var structSig = this.signature.ResolveNames(types);
        var unionSig = new UnionType(structSig.Members);

        types.NominalSignatures.Add(path, unionSig);
    }

    public IDeclaration CheckTypes(TypeFrame types) {
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

        return new UnionDeclaration(this.Location, unionSig, path);
    }
}