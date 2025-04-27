using Helix.Analysis.TypeChecking;
using Helix.Generation;
using Helix.IRGeneration;
using Helix.Parsing;

namespace Helix.Syntax {
    public interface IDeclaration {
        public TokenLocation Location { get; }

        public TypeFrame DeclareNames(TypeFrame names);

        public TypeFrame DeclareTypes(TypeFrame types);

        public DeclarationTypeCheckResult CheckTypes(TypeFrame types);

        public void GenerateIR(IRWriter writer, IRFrame context) {
            throw new InvalidOperationException();
        }
        
        public void GenerateCode(TypeFrame types, ICWriter writer) {
            throw new InvalidOperationException();
        }
    }
}
