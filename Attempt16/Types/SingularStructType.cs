using System.Collections.Generic;
using Attempt16.Analysis;

namespace Attempt16.Types {
    public class SingularStructType : ILanguageType {
        public string Name { get; set; }

        public IdentifierPath ScopePath { get; set; }

        public IReadOnlyList<StructMember> Members { get; set; }

        public T Accept<T>(ITypeVisitor<T> visitor) {
            return visitor.VisitStructType(this);
        }
    }
}