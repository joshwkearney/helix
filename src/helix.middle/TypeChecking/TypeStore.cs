using Helix.Common;
using Helix.Common.Types;

namespace Helix.MiddleEnd.TypeChecking {
    internal class TypeStore {
        private readonly Dictionary<string, IHelixType> types = new();

        public IHelixType this[string name] {
            get => GetType(name);
            set => SetType(name, value);
        }

        public void SetType(string name, IHelixType type) {
            SetTypeMemberHelper("", name, type);
        }

        public void TransferType(string previous, string next) {
            SetType(next, GetType(previous));
        }

        public bool ContainsType(string name) => GetTypeHelper(name).HasValue;

        public IHelixType GetType(string name) => GetTypeHelper(name).GetValue();

        public Option<IHelixType> GetTypeHelper(string name) {
            if (name == "word") {
                return new WordType();
            }
            else if (name == "bool") {
                return new BoolType();
            }
            else if (name == "void") {
                return new VoidType();
            }
            else if (long.TryParse(name, out var w)) {
                return new SingularWordType() { Value = w };
            }
            else if (bool.TryParse(name, out var b)) {
                return new SingularBoolType() { Value = b };
            }
            else {
                return types.GetValueOrNone(name);
            }
        }

        private void SetTypeMemberHelper(string baseName, string name, IHelixType type) {
            if (baseName == string.Empty) {
                types[name] = type;
            }
            else {
                types[baseName + "." + name] = type;
            }

            if (type is StructType structType) {
                foreach (var mem in structType.Members) {
                    SetTypeMemberHelper(baseName + "." + name, mem.Name, mem.Type);
                }
            }

            if (type is StructType unionType) {
                foreach (var mem in unionType.Members) {
                    SetTypeMemberHelper(baseName + "." + name, mem.Name, mem.Type);
                }
            }
        }
    }
}
