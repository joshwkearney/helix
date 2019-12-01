using Attempt16.Types;
using System.Collections.Generic;

namespace Attempt16.Syntax {

    public class StructDeclaration : IDeclaration {
        public string Name { get; set; }

        public IReadOnlyList<StructMember> Members { get; set; }

        public SingularStructType StructType { get; set; }

        public T Accept<T>(IDeclarationVisitor<T> visitor) {
            return visitor.VisitStructDeclaration(this);
        }
    }
}