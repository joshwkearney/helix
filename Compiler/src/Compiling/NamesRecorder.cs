using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Trophy.Compiling {
    public class NamesRecorder : INamesRecorder {
        private int nameCounter = 0;

        private readonly Stack<NamesContext> contexts = new();
        private readonly Stack<Dictionary<IdentifierPath, NameTarget>> names = new();
        private readonly Stack<Dictionary<IdentifierPath, IdentifierPath>> aliases = new();

        public event EventHandler<GenericType> MetaTypeFound;

        public NamesContext Context => this.contexts.Peek();

        public NamesRecorder() {
            this.contexts.Push(new NamesContext(new IdentifierPath(), new IdentifierPath()));
            this.names.Push(new Dictionary<IdentifierPath, NameTarget>());
            this.aliases.Push(new Dictionary<IdentifierPath, IdentifierPath>());
        }

        public void DeclareName(IdentifierPath path, NameTarget target, IdentifierScope scope) {
            if (scope == IdentifierScope.GlobalName) {
                this.names.Last()[path] = target;
            }
            else {
                this.names.Peek()[path] = target;
            }
        }

        public bool TryFindName(string name, out NameTarget target, out IdentifierPath path) {
            var scope = this.contexts.Peek().Scope;

            while (true) {
                path = scope.Append(name);

                if (this.TryGetName(path, out target)) {
                    return true;
                }

                if (!scope.Segments.Any()) {
                    path = new IdentifierPath(name);
                    return this.TryGetName(path, out target);
                }

                scope = scope.Pop();
            }
        }

        public bool TryGetName(IdentifierPath name, out NameTarget target) {
            target = default;

            bool worked = false;

            foreach (var frame in this.names) {
                if (frame.TryGetValue(name, out target)) {
                    worked = true;
                    break;
                }
            }

            if (!worked) {
                foreach (var frame in this.aliases) {
                    if (frame.TryGetValue(name, out var nextPath)) {
                        if (this.TryGetName(nextPath, out target)) {
                            worked = true;
                            break;
                        }
                    }
                }                
            }

            return worked;
        }

        public int GetNewVariableId() {
            return this.nameCounter++;
        }

        public void DeclareAlias(IdentifierPath path, IdentifierPath target, IdentifierScope scope) {
            if (scope == IdentifierScope.GlobalName) {
                this.aliases.Last()[path] = target;
                this.DeclareName(path, NameTarget.Reserved, IdentifierScope.GlobalName);
            }
            else {
                this.aliases.Peek()[path] = target;
                this.DeclareName(path, NameTarget.Reserved, IdentifierScope.LocalName);
            }
        }

        public T WithContext<T>(NamesContext newContext, Func<INamesRecorder, T> recorderFunc) {
            this.names.Push(new Dictionary<IdentifierPath, NameTarget>());
            this.aliases.Push(new Dictionary<IdentifierPath, IdentifierPath>());
            this.contexts.Push(newContext);

            var result = recorderFunc(this);

            this.names.Pop();
            this.aliases.Pop();
            this.contexts.Pop();

            return result;
        }
    }
}