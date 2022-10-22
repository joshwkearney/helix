using Helix.Analysis;
using Helix.Analysis.Types;

namespace Helix.Features.Variables {
    public record VariableSignature {
        public HelixType Type { get; }

        public bool IsWritable { get; }

        public IdentifierPath Path { get; }

        public IReadOnlyList<IdentifierPath> CapturedVariables { get; }

        public VariableSignature(IdentifierPath path, HelixType type, 
            bool isWritable, IReadOnlyList<IdentifierPath> capturedVariables) {

            Path = path;
            Type = type;
            IsWritable = isWritable;
            this.CapturedVariables = capturedVariables;
        }

        public VariableSignature(IdentifierPath path, HelixType type, bool isWritable)
            : this(path, type, isWritable, Array.Empty<IdentifierPath>()) { }
    }
}