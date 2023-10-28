using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.HelixMinusMinus {
    public record HmmValue {
        private readonly object value;
        private readonly ValueType type;

        public static HmmValue Void { get; } = new HmmValue(null, ValueType.VoidLiteral);
        public static HmmValue Variable(HmmVariable value) => new HmmValue(value, ValueType.Name);
        public static HmmValue Word(long value) => new HmmValue(value, ValueType.WordLiteral);
        public static HmmValue Bool(bool value) => new HmmValue(value, ValueType.BoolLiteral);

        public bool IsVoid => this.type == ValueType.VoidLiteral;

        private enum ValueType {
            Name,
            WordLiteral,
            BoolLiteral,
            VoidLiteral
        }

        private HmmValue(object value, ValueType type) {
            this.value = value;
            this.type = type;
        }

        public Option<HmmVariable> AsVariable() {
            if (this.type == ValueType.Name) {
                return (HmmVariable)this.value;
            }
            else {
                return Option.None;
            }
        }

        public Option<int> AsInt() {
            if (this.type == ValueType.WordLiteral) {
                return (int)this.value;
            }
            else {
                return Option.None;
            }
        }

        public Option<bool> AsBool() {
            if (this.type == ValueType.BoolLiteral) {
                return (bool)this.value;
            }
            else {
                return Option.None;
            }
        }

        public static implicit operator HmmValue(HmmVariable var) {
            return HmmValue.Variable(var);
        }

        public override string ToString() {
            return this.type switch {
                ValueType.Name => ((HmmVariable)this.value).Name,
                ValueType.BoolLiteral => (bool)this.value ? "true" : "false",
                ValueType.VoidLiteral => "void",
                ValueType.WordLiteral => this.value.ToString(),
                _ => throw new InvalidOperationException()
            };
        }
    }
}
