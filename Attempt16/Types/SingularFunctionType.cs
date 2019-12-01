using System.Collections.Generic;
using System.Linq;
using Attempt16.Analysis;

namespace Attempt16.Types {
    public class SingularFunctionType : ILanguageType {
        public IdentifierPath ReturnTypePath { get; set; }

        public IReadOnlyList<FunctionParameter> Parameters { get; set; }

        public IdentifierPath ScopePath { get; set; }

        public T Accept<T>(ITypeVisitor<T> visitor) {
            return visitor.VisitFunctionType(this);
        }
    }
}