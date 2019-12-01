using Attempt12.TypeSystem;

namespace Attempt12.Analyzing {
    public class VariableLocation {
        private static int id = 0;

        public ISymbol Type { get; }

        public bool IsImmutable { get; }

        public string Name { get; }

        public int Id { get; }

        public VariableLocation(string name, ISymbol type, bool isImmutable = true) {
            this.Name = name;
            this.Type = type;
            this.IsImmutable = isImmutable;
            this.Id = id++;
        }

        public override string ToString() {
            return $"{this.Name}, Id= {this.Id}";
        }
    }
}