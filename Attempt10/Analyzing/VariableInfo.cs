namespace Attempt12 {
    public class VariableInfo {
        public string Name { get; }

        public ITrophyType Type { get; }

        public int DefinedClosureLevel { get; }

        public VariableInfo(string name, ITrophyType type, int closureLevel) {
            this.Type = type;
            this.DefinedClosureLevel = closureLevel;
            this.Name = name;
        }
    }
}