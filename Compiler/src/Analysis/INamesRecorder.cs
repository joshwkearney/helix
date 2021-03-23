using System;
using Trophy.Analysis.Types;
using Trophy.Parsing;

namespace Trophy.Analysis {
    public enum IdentifierScope {
        LocalName, GlobalName
    }

    public interface INamesRecorder {
        public NamesContext Context { get; }

        public int GetNewVariableId();

        public void DeclareName(IdentifierPath path, NameTarget target, IdentifierScope scope);

        public void DeclareAlias(IdentifierPath path, IdentifierPath target, IdentifierScope scope);

        public bool TryGetName(IdentifierPath path, out NameTarget nameTarget);

        public bool TryFindName(string name, out NameTarget nameTarget, out IdentifierPath path);

        public T WithContext<T>(NamesContext newContext, Func<INamesRecorder, T> recorderFunc);
    }

    public enum NameTarget {
        Variable, Function, Region, Struct, Union, Reserved, GenericType
    }

    public class NamesContext {
        public IdentifierPath Scope { get; }

        public IdentifierPath Region { get; }

        public NamesContext(IdentifierPath scope, IdentifierPath region) {
            this.Scope = scope;
            this.Region = region;
        }

        public NamesContext WithScope(Func<IdentifierPath, IdentifierPath> scopeFunc) {
            return new NamesContext(scopeFunc(this.Scope), this.Region);
        }

        public NamesContext WithRegion(Func<IdentifierPath, IdentifierPath> scopeFunc) {
            return new NamesContext(this.Scope, scopeFunc(this.Region));
        }
    }
}