using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Structs;

public record StructParseDeclaration : IDeclaration {
    public required TokenLocation Location { get; init; }
    
    public required ParseStructSignature Signature { get; init; }

    public TypeFrame DeclareNames(TypeFrame types) {
        // Make sure this name isn't taken
        if (types.TryResolvePath(types.Scope, this.Signature.Name, out _)) {
            throw TypeException.IdentifierDefined(this.Location, this.Signature.Name);
        }

        var path = types.Scope.Append(this.Signature.Name);
        var named = new NominalType(path, NominalTypeKind.Struct);

        return types.WithDeclaration(path, DeclarationKind.Type, named);
    }

    public TypeFrame DeclareTypes(TypeFrame types) {
        var path = types.Scope.Append(this.Signature.Name);
        var sig = this.Signature.ResolveNames(types);
        
        return types.WithNominalSignature(path, sig);
    }

    public DeclarationTypeCheckResult CheckTypes(TypeFrame types) {
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

        var result = new StructDeclaration {
            Location = this.Location,
            Signature = sig,
            Path = path
        };

        return new(result, types);
    }
}