using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Unions {
    public class FlowVarParseSyntax : IParseSyntax {
        public required StructMember UnionMember { get; init; }
        
        public required IdentifierPath ShadowedPath { get; init; }

        public required PointerType ShadowedType { get; init; }
        
        public required TokenLocation Location { get; init; }
        
        public bool IsPure => true;
        
        public ISyntax CheckTypes(TypeFrame types) {
            var varSig = new PointerType(this.UnionMember.Type);
            var path = types.Scope.Append(this.ShadowedPath.Segments.Last());

            types.Locals = types.Locals.SetItem(path, new LocalInfo(varSig));
            types.NominalSignatures.Add(path, varSig);

            var result = new FlowVarSyntax {
                Location = this.Location,
                UnionMember = this.UnionMember,
                ShadowedPath = this.ShadowedPath,
                Path = path
            };
            
            return result;
        }
    }

}
