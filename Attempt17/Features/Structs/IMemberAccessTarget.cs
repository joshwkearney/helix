using System.Collections.Immutable;
using Attempt17.TypeChecking;

namespace Attempt17.Features.Structs {
    public interface IMemberAccessTarget {
        public IMemberAccessTarget AccessMember(string name);

        public IMemberAccessTarget InvokeMember(string name, ImmutableList<ISyntax<TypeCheckTag>> arguments);

        public ISyntax<TypeCheckTag> ToSyntax();
    }
}