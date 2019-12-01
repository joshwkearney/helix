namespace Attempt9 {
    public class IfExpression : IParseTree {
        public IParseTree Condition { get; }

        public IParseTree AffirmativeExpression { get; }

        public IParseTree NegativeExpression { get; }

        public IfExpression(IParseTree condition, IParseTree affirm, IParseTree neg) {
            this.Condition = condition;
            this.AffirmativeExpression = affirm;
            this.NegativeExpression = neg;
        }

        public void Accept(IParseTreeVisitor visitor) => visitor.Visit(this);
    }
}