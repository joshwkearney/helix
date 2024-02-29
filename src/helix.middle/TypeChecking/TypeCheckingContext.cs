using Helix.Common.Hmm;
using Helix.MiddleEnd.Unification;

namespace Helix.MiddleEnd.TypeChecking {
    internal class TypeCheckingContext {
        public required HmmWriter Writer { get; init; }

        public required TypeStore Types { get; init; }

        public required TypeCheckingNamesStore Names { get; init; }

        public TypeChecker TypeChecker { get; }

        public TypeUnifier Unifier { get; }

        public TypeCheckingContext() {
            TypeChecker = new TypeChecker(this);
            Unifier = new TypeUnifier(this);
        }
    }
}
