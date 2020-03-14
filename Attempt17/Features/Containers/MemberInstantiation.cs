namespace Attempt17.Features.Containers {
    public class MemberInstantiation<T> {
        public string MemberName { get; }

        public ISyntax<T> Value { get; }

        public MemberInstantiation(string name, ISyntax<T> value) {
            this.MemberName = name;
            this.Value = value;
        }
    }
}