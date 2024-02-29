using Helix.Analysis.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.MiddleEnd {
    internal class TypeStore {
        private readonly Dictionary<string, IHelixType> types = new();

        public IHelixType this[string name] {
            get => this.GetType(name);
            set => this.SetType(name, value);
        }

        public void SetType(string name, IHelixType type) {
            this.SetTypeMemberHelper("", name, type);
        }
        
        public void TransferType(string previous, string next) {
            this.SetType(next,  this.GetType(previous));
        }

        public bool ContainsType(string name) => this.GetTypeHelper(name).HasValue;

        public IHelixType GetType(string name) => this.GetTypeHelper(name).GetValue();

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
                return this.types.GetValueOrNone(name);
            }
        }

        private void SetTypeMemberHelper(string baseName, string name, IHelixType type) {
            if (baseName == string.Empty) {
                this.types[name] = type;
            }
            else {
                this.types[baseName + "." + name] = type;
            }

            if (type is StructType structType) {
                foreach (var mem in structType.Members) {
                    this.SetTypeMemberHelper(baseName + "." + name, mem.Name, mem.Type);
                }
            }

            if (type is StructType unionType) {
                foreach (var mem in unionType.Members) {
                    this.SetTypeMemberHelper(baseName + "." + name, mem.Name, mem.Type);
                }
            }
        }
    }
}
