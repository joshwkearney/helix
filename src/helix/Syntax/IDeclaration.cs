using Helix.CodeGeneration;
using Helix.FlowAnalysis;
using Helix.Parsing;
using Helix.TypeChecking;

namespace Helix.Syntax {
    public interface IDeclaration {
        public TokenLocation Location { get; }

        public TypeFrame DeclareNames(TypeFrame names);

        public TypeFrame DeclareTypes(TypeFrame types);

        public TypeCheckResult<IDeclaration> CheckTypes(TypeFrame types);

        public void GenerateIR(IRWriter writer, IRFrame context) {
            throw new NotImplementedException();
        }
        
        public void GenerateCode(TypeFrame types, ICWriter writer) {
            throw new NotImplementedException();
        }
    }
}
