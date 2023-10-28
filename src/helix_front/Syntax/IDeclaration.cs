using Helix.Analysis.TypeChecking;
using Helix.Generation;
using Helix.HelixMinusMinus;
using Helix.Parsing;

namespace Helix.Syntax
{
    public interface IDeclaration {
        public TokenLocation Location { get; }

        public void DeclareNames(TypeFrame names);

        public void DeclareTypes(TypeFrame types);

        public IDeclaration CheckTypes(TypeFrame types);

        public void GenerateCode(TypeFrame types, ICWriter writer) {
            throw new InvalidOperationException();
        }

        public void GenerateHelixMinusMinus(TypeFrame types, HmmWriter writer) {
            throw new Exception("Compiler bug");
        }
    }
}
