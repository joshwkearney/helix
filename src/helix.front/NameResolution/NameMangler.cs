using Helix.Common;
using System.IO;

namespace Helix.Frontend.NameResolution {
    internal class NameMangler {
        private int counter = 0;
        private readonly Dictionary<IdentifierPath, string> mangledNames = [];

        public NameMangler() {
        }

        public string MangleLocalName(IdentifierPath scope, string name) {
            return this.MangleLocalName(scope.Append(name));
        }

        public string MangleLocalName(IdentifierPath path) {
            Assert.IsFalse(mangledNames.ContainsKey(path));

            mangledNames[path] = path.Segments.Last() + "_" + counter++;
            return mangledNames[path];
        }

        public string MangleGlobalName(IdentifierPath scope, string name) {
            return this.MangleGlobalName(scope.Append(name));
        }

        public string MangleGlobalName(IdentifierPath path) {
            Assert.IsFalse(mangledNames.ContainsKey(path));

            mangledNames[path] = string.Join("_", path.Segments);
            return mangledNames[path];
        }

        public string CreateMangledTempName(IdentifierPath scope, string name = "temp") {
            var path = scope.Append(name + "_" + this.counter++);

            mangledNames[path] = path.Segments.Last();
            return mangledNames[path];
        }

        public string GetMangledName(IdentifierPath path) {
            return mangledNames[path];
        }

        public string GetMangledName(IdentifierPath scope, string name) {
            return this.GetMangledName(scope.Append(name));
        }
    }
}
