using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Structs;

public record StructParseDeclaration : IDeclaration {
    public required TokenLocation Location { get; init; }
    
    public required ParseStructSignature Signature { get; init; }

    public void DeclareNames(TypeFrame types) {
        // Make sure this name isn't taken
        if (types.TryResolvePath(types.Scope, this.Signature.Name, out _)) {
            throw TypeException.IdentifierDefined(this.Location, this.Signature.Name);
        }

        var path = types.Scope.Append(this.Signature.Name);
        var named = new NominalType(path, NominalTypeKind.Struct);

        types.Locals = types.Locals.SetItem(path, new LocalInfo(named));
    }

    public void DeclareTypes(TypeFrame types) {
        var path = types.Scope.Append(this.Signature.Name);
        var sig = this.Signature.ResolveNames(types);

        types.NominalSignatures.Add(path, sig);
    }

    public IDeclaration CheckTypes(TypeFrame types) {
        var path = types.Scope.Append(this.Signature.Name);
        var named = new NominalType(path, NominalTypeKind.Struct);
        var sig = named.AsStruct(types).GetValue();

        var isRecursive = sig.Members
            .Select(x => x.Type)
            .Where(x => x.IsValueType(types))
            .SelectMany(x => x.GetAccessibleTypes(types))
            .Contains(named);

        // Make sure this is not a recursive struct or union
        if (isRecursive) {
            throw TypeException.CircularValueObject(this.Location, named);
        }

        return new StructDeclaration {
            Location = this.Location,
            Signature = sig,
            Path = path
        };
    }
}