using System.Collections.Generic;
using Attempt16.Analysis;
using Attempt16.Types;

namespace Attempt16.Syntax {
    public class StructMemberInitialization {
        public string MemberName { get; set; }

        public ISyntax Value { get; set; }

        public DeclarationOperation Operation { get; set; }
    }

    public class StructInitializationSyntax : ISyntax {
        public ILanguageType ReturnType { get; set; }

        public HashSet<IdentifierPath> ReturnCapturedVariables { get; set; }

        public string StructName { get; set; }

        public IReadOnlyList<StructMemberInitialization> Members { get; set; }
        
        public SingularStructType StructType { get; set; }

        public T Accept<T>(ISyntaxVisitor<T> visitor) {
            return visitor.VisitStructInitialization(this);
        }
    }
}