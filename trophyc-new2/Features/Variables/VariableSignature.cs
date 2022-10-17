namespace Trophy.Features.Variables
{
    public record VariableSignature
    {
        public TrophyType Type { get; }

        public bool IsWritable { get; }

        public IdentifierPath Path { get; }

        public VariableSignature(IdentifierPath path, TrophyType type, bool isWritable)
        {
            Path = path;
            Type = type;
            IsWritable = isWritable;
        }
    }
}