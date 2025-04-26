using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.FlowControl.ParseSyntax;
using Helix.Features.Functions.Syntax;
using Helix.Features.Primitives.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Functions.ParseSyntax;

public record FunctionParseDeclaration : IDeclaration {
    private readonly ParseFunctionSignature signature;
    private readonly IParseSyntax body;

    public TokenLocation Location { get; }

    public FunctionParseDeclaration(TokenLocation loc, ParseFunctionSignature sig, IParseSyntax body) {

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
        
    public DeclarationTypeCheckResult CheckTypes(TypeFrame types) {
        var path = types.ResolvePath(types.Scope, this.signature.Name);
        var sig = new NominalType(path, NominalTypeKind.Function).AsFunction(types).GetValue();

        // Set the scope for type checking the body
        types = types.WithScope(this.signature.Name);

        // Declare parameters
        types = FunctionsHelper.DeclareParameters(sig, path, types);
            
        // Check types
        (var checkedBody, types) = this.body.CheckTypes(types);
        checkedBody = checkedBody.UnifyTo(sig.ReturnType, types);
        
        // Make sure we always return a value
        if (sig.ReturnType != PrimitiveType.Void && !checkedBody.AlwaysJumps) {
            throw new InvalidOperationException("Function does not return a value");
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