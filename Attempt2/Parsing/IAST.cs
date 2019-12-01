using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt2.Parsing {
    public interface IAST {
        void Accept(ISyntaxVisitor visitor);
    }
}