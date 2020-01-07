using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Experimental.Features.Variables {
    public class VariableAccessParseSyntax : ISyntax<ParseInfo> {
        public ParseInfo Tag { get; }

        public string VariableName { get; }

        public VariableAccessKind Kind { get; }

        public VariableAccessParseSyntax(ParseInfo tag, VariableAccessKind kind, string variableName) {
            this.Tag = tag;
            this.Kind = kind;
            this.VariableName = variableName;
        }
    }
}