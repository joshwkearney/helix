using Attempt16.Syntax;
using Attempt16.Types;

namespace Attempt16.Analysis {
    public class VariableInfo {
        public VariableSource Source { get; set; }

        public ILanguageType Type { get; set; }

        public IdentifierPath ScopePath { get; set; }
    }
}