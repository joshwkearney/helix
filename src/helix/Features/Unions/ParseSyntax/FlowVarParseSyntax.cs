using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.Unions.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Unions.ParseSyntax {
    public class FlowVarParseSyntax : IParseSyntax {
        public required StructMember UnionMember { get; init; }
        
        public required IdentifierPath ShadowedPath { get; init; }

        public required PointerType ShadowedType { get; init; }
        
        public required TokenLocation Location { get; init; }
        
        public bool IsPure => true;
        
        public TypeCheckResult CheckTypes(TypeFrame types) {
            var varSig = new PointerType(this.UnionMember.Type);
            var path = types.Scope.Append(this.ShadowedPath.Segments.Last());

            types = types.WithDeclaration(path, DeclarationKind.Variable, varSig);

            var result = new FlowVarSyntax {
                Location = this.Location,
                UnionMember = this.UnionMember,
                ShadowedPath = this.ShadowedPath,
                Path = path
            };
            
            return new TypeCheckResult(result, types);
        }
    }
}
