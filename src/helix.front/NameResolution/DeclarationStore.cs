using Helix.Common;
using Helix.Common.Types;
using System.Diagnostics.CodeAnalysis;

namespace Helix.Frontend.NameResolution {
    internal class DeclarationStore {
        private readonly HashSet<IdentifierPath> names = [];
        private readonly Dictionary<IdentifierPath, IHelixType> types = [];

        public DeclarationStore() { }


        public void SetDeclaration(IdentifierPath path) {
            names.Add(path);
        }

        public void SetDeclaration(IdentifierPath scope, string name) {
            this.SetDeclaration(scope.Append(name));
        }

        public void SetDeclaration(IdentifierPath path, IHelixType signature) {
            names.Add(path);
            types[path] = signature;
        }

        public void SetDeclaration(IdentifierPath scope, string name, IHelixType signature) {
            this.SetDeclaration(scope.Append(name), signature);
        }        

        public bool ContainsDeclaration(IdentifierPath path) => this.names.Contains(path);

        public Option<IdentifierPath> ResolveDeclaration(IdentifierPath scope, string name) {
            if (this.TryResolveDeclarationHelper(scope, name, out var path, out _)) {
                return path;
            }
            else {
                return Option.None;
            }
        }

        public Option<IHelixType> ResolveSignature(IdentifierPath scope, string name) {
            return this.ResolveDeclaration(scope, name).SelectMany(x => this.types.GetValueOrNone(x));
        }

        public Option<IHelixType> GetSignature(IdentifierPath path) {
            return this.types.GetValueOrNone(path);
        }

        private bool TryResolveDeclarationHelper(IdentifierPath scope, string name, [NotNullWhen(true)] out IdentifierPath? path, [NotNullWhen(true)] out Option<IHelixType> kind) {
            while (true) {
                path = scope.Append(name);

                if (this.names.Contains(path)) {
                    kind = this.types.GetValueOrNone(path);
                    return true;
                }

                if (!scope.Segments.IsEmpty) {
                    scope = scope.Pop();
                }
                else {
                    kind = Option.None;
                    return false;
                }
            }
        }
    }
}
