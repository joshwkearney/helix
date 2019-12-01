namespace Attempt4 {
    public class IntegerLiteral : ISyntaxTree, IInterpretedValue<int> {
        public int Value { get; }

        object IInterpretedValue.Value => this.Value;

        public LanguageType ExpressionType => LanguageType.Int32Type;

        public IntegerLiteral(int value) {
            this.Value = value;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }

        public void Accept(IAnalyzedSyntaxVisitor visitor) {
            visitor.Visit(this);
        }

        public override string ToString() {
            return this.Value.ToString();
        }
    }
}