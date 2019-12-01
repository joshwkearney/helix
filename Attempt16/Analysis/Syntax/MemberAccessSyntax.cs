using System;
using System.Collections.Generic;
using System.Text;
using Attempt16.Analysis;
using Attempt16.Types;

namespace Attempt16.Syntax {
    public class MemberAccessSyntax : ISyntax {
        public ISyntax Target { get; set; }
        public string MemberName { get; set; }

        public bool IsLiteralAccess { get; set; }
        public ILanguageType ReturnType { get; set; }
        public HashSet<IdentifierPath> ReturnCapturedVariables { get; set; }
        public T Accept<T>(ISyntaxVisitor<T> visitor) {
            return visitor.VisitMemberAccessSyntax(this);
        }
    }
}