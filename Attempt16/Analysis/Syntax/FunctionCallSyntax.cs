using System.Collections.Generic;
using Attempt16.Analysis;
using Attempt16.Types;

namespace Attempt16.Syntax {
    public class FunctionCallSyntax : ISyntax {
        public ILanguageType ReturnType { get; set; }

        public HashSet<IdentifierPath> ReturnCapturedVariables { get; set; }

        public ISyntax Target { get; set; }

        public IReadOnlyList<ISyntax> Arguments { get; set; }

        public T Accept<T>(ISyntaxVisitor<T> visitor) {
            return visitor.VisitFunctionCall(this);
        }
    }
}
