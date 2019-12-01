using Attempt16.Analysis;

namespace Attempt16.Types {
    public class FunctionParameter {
        public string Name { get; set; }

        public IdentifierPath TypePath { get; set; }

        public void Deconstruct(out IdentifierPath type, out string name) {
            type = this.TypePath;
            name = this.Name;
        }
    }
}