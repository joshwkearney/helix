using System.Collections.Immutable;

namespace Attempt19.Features.Containers {
    public interface IMemberAccessTarget {
        public IMemberAccessTarget AccessMember(string name);

        public IMemberAccessTarget InvokeMember(string name, ISyntax[] arguments);

        public ISyntax ToSyntax();
    }
}