using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Trophy.Compiling {
    public class NamesRecorder : INameRecorder {
        private readonly Stack<IdentifierPath> scopes = new Stack<IdentifierPath>();
        private readonly Stack<IdentifierPath> regions = new Stack<IdentifierPath>();
        private int nameCounter = 0;

        public event EventHandler<GenericType> MetaTypeFound;

        private readonly Dictionary<IdentifierPath, NameTarget> globalNames
                    = new Dictionary<IdentifierPath, NameTarget>();

        private readonly Stack<Dictionary<IdentifierPath, NameTarget>> localNames
            = new Stack<Dictionary<IdentifierPath, NameTarget>>();

        private readonly Dictionary<IdentifierPath, IdentifierPath> aliases 
            = new Dictionary<IdentifierPath, IdentifierPath>();

        public IdentifierPath CurrentScope => this.scopes.Peek();

        public IdentifierPath CurrentRegion => this.regions.Peek();

        public NamesRecorder() {
            this.scopes.Push(new IdentifierPath());
            this.regions.Push(new IdentifierPath());
            this.localNames.Push(new Dictionary<IdentifierPath, NameTarget>());
        }

        public void DeclareGlobalName(IdentifierPath path, NameTarget target) {
            this.globalNames[path] = target;
        }

        public void DeclareLocalName(IdentifierPath path, NameTarget target) {
            this.localNames.Peek()[path] = target;
        }

        public void PopScope() {
            this.scopes.Pop();
            this.localNames.Pop();
        }

        public void PushScope(IdentifierPath newScope) {
            this.scopes.Push(newScope);
            this.localNames.Push(new Dictionary<IdentifierPath, NameTarget>());
        }

        public bool TryFindName(string name, out NameTarget target, out IdentifierPath path) {
            var scope = this.CurrentScope;

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

            foreach (var frame in this.localNames) {
                if (frame.TryGetValue(name, out target)) {
                    worked = true;
                    break;
                }
            }

            if (!worked) {
                if (this.globalNames.TryGetValue(name, out target)) {
                    worked = true;
                }
            }

            if (!worked) {
                if (this.aliases.TryGetValue(name, out var nextPath)) {
                    worked = this.TryGetName(nextPath, out target);
                }
            }

            return worked;
        }

        public void PushRegion(IdentifierPath newRegion) {
            this.regions.Push(newRegion);
        }

        public void PopRegion() {
            this.regions.Pop();
        }

        public int GetNewVariableId() {
            return this.nameCounter++;
        }

        public void DeclareGlobalAlias(IdentifierPath path, IdentifierPath target) {
            this.aliases[path] = target;
            this.DeclareGlobalName(path, NameTarget.Alias);
        }

        public void DeclareLocalAlias(IdentifierPath path, IdentifierPath target) {
            this.aliases[path] = target;
            this.DeclareLocalName(path, NameTarget.Alias);
        }
    }
}