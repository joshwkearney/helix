using Attempt16.Analysis;
using Attempt16.Types;
using System.Collections.Generic;

namespace Attempt16.Syntax {
    public interface ISyntax {
        ILanguageType ReturnType { get; set; }

        HashSet<IdentifierPath> ReturnCapturedVariables { get; set; }
        
        T Accept<T>(ISyntaxVisitor<T> visitor);
    }
}
