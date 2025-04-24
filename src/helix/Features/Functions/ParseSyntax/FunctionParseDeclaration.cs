using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.FlowControl;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Functions;

public record FunctionParseDeclaration : IDeclaration {
    private readonly ParseFunctionSignature signature;
    private readonly IParseSyntax body;

    public TokenLocation Location { get; }

    public FunctionParseDeclaration(TokenLocation loc, ParseFunctionSignature sig, IParseSyntax body) {

        this.Location = loc;
        this.signature = sig;
        this.body = body;
    }

    public void DeclareNames(TypeFrame types) {
        FunctionsHelper.CheckForDuplicateParameters(
            this.Location, 
            this.signature.Parameters.Select(x => x.Name));

        FunctionsHelper.DeclareName(this.signature, types);
    }

    public void DeclareTypes(TypeFrame types) {
        var path = types.Scope.Append(this.signature.Name);
        var sig = this.signature.ResolveNames(types);
        var named = new NominalType(path, NominalTypeKind.Function);

        types.Locals = types.Locals.SetItem(path, new LocalInfo(named));

        // Declare this function
        types.NominalSignatures.Add(path, sig);
    }
        
    public IDeclaration CheckTypes(TypeFrame types) {
        var path = types.ResolvePath(types.Scope, this.signature.Name);
        var sig = new NominalType(path, NominalTypeKind.Function).AsFunction(types).GetValue();

        // Set the scope for type checking the body
        types = new TypeFrame(types, this.signature.Name);

        // Declare parameters
        FunctionsHelper.DeclareParameters(sig, path, types);
            
        // Check types
        var body = this.body;

        if (sig.ReturnType == PrimitiveType.Void) {
            body = new BlockParse(
                this.body.Location, 
                this.body,
                new VoidLiteral { Location = this.body.Location }
            );
        }

        var checkedbody = body.CheckTypes(types)
            .ToRValue(types)
            .UnifyTo(sig.ReturnType, types);

        return new FunctionDeclaration {
            Location = this.Location,
            Path = path,
            Signature = sig,
            Body = checkedbody
        };
    }
}