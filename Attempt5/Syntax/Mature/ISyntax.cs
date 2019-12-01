using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt6.Syntax {
    public interface ISyntax {
        ILanguageType ExpressionType { get; }
        void Accept(ISyntaxVisitor visitor);
    }
}