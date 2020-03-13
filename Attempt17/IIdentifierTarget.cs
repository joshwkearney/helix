using Attempt17.Types;

namespace Attempt17 {
    public interface IIdentifierTarget {
        public IdentifierPath Path { get; }

        public LanguageType Type { get; }

        public T Accept<T>(IIdentifierTargetVisitor<T> visitor);

        public IOption<VariableInfo> AsVariable() {
            return this.Accept(new IdentifierTargetVisitor<IOption<VariableInfo>>() {
                HandleComposite = _ => Option.None<VariableInfo>(),
                HandleFunction = _ => Option.None<VariableInfo>(),
                HandleVariable = Option.Some
            });
        }

        public IOption<FunctionInfo> AsFunction() {
            return this.Accept(new IdentifierTargetVisitor<IOption<FunctionInfo>>() {
                HandleComposite = _ => Option.None<FunctionInfo>(),
                HandleFunction = Option.Some,
                HandleVariable = _ => Option.None<FunctionInfo>()
            });
        }

        public IOption<CompositeInfo> AsComposite() {
            return this.Accept(new IdentifierTargetVisitor<IOption<CompositeInfo>>() {
                HandleComposite = Option.Some,
                HandleFunction = _ => Option.None<CompositeInfo>(),
                HandleVariable = _ => Option.None<CompositeInfo>()
            });
        }
    }
}