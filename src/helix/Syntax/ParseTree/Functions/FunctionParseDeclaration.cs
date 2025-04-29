using Helix.Parsing;
using Helix.Syntax.TypedTree.Functions;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Functions;

public record FunctionParseDeclaration : IDeclaration {
    private readonly ParseFunctionSignature signature;
    private readonly IParseStatement body;

    public TokenLocation Location { get; }

    public FunctionParseDeclaration(TokenLocation loc, ParseFunctionSignature sig, IParseStatement body) {
        this.Location = loc;
        this.signature = sig;
        this.body = body;
    }

    public TypeFrame DeclareNames(TypeFrame types) {
        FunctionsHelper.CheckForDuplicateParameters(
            this.Location, 
            this.signature.Parameters.Select(x => x.Name));

        return FunctionsHelper.DeclareName(this.signature, types);
    }

    public TypeFrame DeclareTypes(TypeFrame types) {
        var path = types.Scope.Append(this.signature.Name);
        var sig = this.signature.ResolveNames(types);

        return types.WithSignature(path, sig);
    }
        
    public TypeCheckResult<IDeclaration> CheckTypes(TypeFrame types) {
        var path = types.ResolvePath(types.Scope, this.signature.Name);
        var sig = new NominalType(path, NominalTypeKind.Function).AsFunction(types).GetValue();

        // Set the scope for type checking the body
        types = types.WithScope(this.signature.Name);

        // Declare parameters
        types = FunctionsHelper.DeclareParameters(sig, path, types);
            
        // Check types
        (var checkedBody, types) = this.body.CheckTypes(types);
        
        // Make sure we always return a value
        if (sig.ReturnType != PrimitiveType.Void && !checkedBody.AlwaysJumps) {
            throw TypeException.NoReturn(this.signature.Location, this.signature.Name);
        }

        var result = new FunctionDeclaration {
            Location = this.Location,
            Path = path,
            Signature = sig,
            Body = checkedBody
        };

        return new(result, types);
    }
}