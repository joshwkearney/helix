namespace Trophy.Parsing.ParseTree {
    public interface ITypeTree {
        public TokenLocation Location { get; }

        public TrophyType ResolveNames(IdentifierPath scope, NamesRecorder names);
    }

    public class PrimitiveTypeTree : ITypeTree {
        public TokenLocation Location { get; }

        public TrophyType PrimitiveType { get; }

        public PrimitiveTypeTree(TokenLocation loc, TrophyType type) {
            this.Location = loc;
            this.PrimitiveType = type;
        }

        public TrophyType ResolveNames(IdentifierPath scope, NamesRecorder names) {
            return this.PrimitiveType;
        }
    }

    public class TypeVariableAccess : ITypeTree {
        public TokenLocation Location { get; }

        public string Name { get; }

        public TypeVariableAccess(TokenLocation loc, string name) {
            this.Location = loc;
            this.Name = name;
        }

        public TrophyType ResolveNames(IdentifierPath scope, NamesRecorder names) {
            if (!names.TryFindName(scope, this.Name).TryGetValue(out var path)) {
                throw new Exception();
            }

            if (!names.TryGetName(path).TryGetValue(out var target)) {
                throw new Exception("This should never happen");
            }

            if (target == NameTarget.Variable || target == NameTarget.Function) {
                // TODO: Put actual error here
                throw new Exception();
            }

            return new NamedType(scope.Append(this.Name));
        }
    }
}
