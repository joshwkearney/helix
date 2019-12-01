using System.Collections.Generic;

namespace Attempt16.Syntax {
    public class CompilationUnit {
        public string FileName { get; set; }

        public IReadOnlyList<IDeclaration> Declarations { get; set; }
    }
}