using Attempt17.Types;

namespace Attempt17.Types {
    public class FunctionParameter {
        public string Name { get; }

        public LanguageType Type { get; }

        public FunctionParameter(string name, LanguageType type) {
            this.Name = name;
            this.Type = type;
        }
    }
}