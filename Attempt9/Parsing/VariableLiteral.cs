namespace Attempt9 {
    public class VariableLiteral : IParseTree {
        public string Name { get; }

        public VariableLiteral(string name) {
            this.Name = name;
        }

        public void Accept(IParseTreeVisitor visitor) => visitor.Visit(this);
    }
}