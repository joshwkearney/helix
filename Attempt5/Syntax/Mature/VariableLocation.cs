using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt6.Syntax {
    public class VariableLocation : ISyntax {
        public string Name { get; }

        public bool IsReadOnly { get; }

        public ILanguageType ExpressionType { get; }

        public VariableLocation(string name, bool isreadonly, ILanguageType type) {
            this.Name = name;
            this.IsReadOnly = isreadonly;
            this.ExpressionType = type;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}