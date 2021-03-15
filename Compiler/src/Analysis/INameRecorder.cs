using Trophy.Analysis.Types;
using Trophy.Parsing;

namespace Trophy.Analysis {
    public interface INameRecorder {
        public IdentifierPath CurrentScope { get; }

        public IdentifierPath CurrentRegion { get; }

        public int GetNewVariableId();

        public void DeclareGlobalName(IdentifierPath path, NameTarget target);

        public void DeclareLocalName(IdentifierPath path, NameTarget target);

        public bool TryGetName(IdentifierPath path, out NameTarget nameTarget);

        public bool TryFindName(string name, out NameTarget nameTarget, out IdentifierPath path);

        public void PushScope(IdentifierPath newScope);

        public void PopScope();

        public void PushRegion(IdentifierPath newRegion);

        public void PopRegion();

        public TrophyType ResolveTypeNames(TrophyType type, TokenLocation loc);
    }

    public enum NameTarget {
        Variable, Function, Region, Struct, Union, Reserved, Metatype, TypeAlias
    }
}