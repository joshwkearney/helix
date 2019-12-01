using Attempt16.Types;
using System.Collections.Immutable;

namespace Attempt16.Generation {
    public class CType {
        public string CTypeName { get; set; }

        public ImmutableList<string> HeaderLines { get; set; }
    }
}