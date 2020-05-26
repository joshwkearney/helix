using System.Collections.Generic;

namespace Attempt19 {
    public class NameCache {
        private readonly Dictionary<IdentifierPath, NameTarget> globalNames
            = new Dictionary<IdentifierPath, NameTarget>();

        private readonly Stack<Dictionary<IdentifierPath, NameTarget>> localNames
            = new Stack<Dictionary<IdentifierPath, NameTarget>>();

        public NameCache() { }

        public void AddGlobalName(IdentifierPath path, NameTarget target) {
            this.globalNames.Add(path, target);
        }

        public void AddLocalName(IdentifierPath path, NameTarget target) {
            this.localNames.Peek().Add(path, target);
        }

        public void PushLocalFrame() {
            this.localNames.Push(new Dictionary<IdentifierPath, NameTarget>());
        }

        public void PopLocalFrame() {
            this.localNames.Pop();
        }

        public bool GetName(IdentifierPath name, out NameTarget target) {
            foreach (var frame in this.localNames) {
                if (frame.TryGetValue(name, out target)) {
                    return true;
                }
            }

            return this.globalNames.TryGetValue(name, out target);
        }

        public bool FindName(IdentifierPath scope, string name, out IdentifierPath path, out NameTarget target) {
            while (true) {
                path = scope.Append(name);

                if (this.GetName(path, out target)) {
                    return true;
                }

                if (scope.Segments.IsEmpty) {
                    return false;
                }

                scope = scope.Pop();
            }
        }
    }
}