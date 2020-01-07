namespace Attempt17.Features.FlowControl {
    public class IfSyntax<T> : ISyntax<T> {
        public T Tag { get; }

        public IfKind Kind { get; }

        public ISyntax<T> Condition { get; }

        public ISyntax<T> Affirmative { get; }

        public IOption<ISyntax<T>> Negative { get; }

        public IfSyntax(T tag, IfKind kind, ISyntax<T> condition, ISyntax<T> affirmative, IOption<ISyntax<T>> negative) {
            this.Tag = tag;
            this.Kind = kind;
            this.Condition = condition;
            this.Affirmative = affirmative;
            this.Negative = negative;
        }
    }
}