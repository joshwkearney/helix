using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt13 {
    public enum DataKind {
        Symbol, Integer, Dictionary
    }

    public abstract class Data {
        public static Data From(int value) {
            return new IntegerData(value);
        }

        public static Data From(string symbol) {
            return new SymbolData(symbol);
        }

        public static Data From(IDictionary<Data, IList<Data>> dict) {
            return new DictionaryData(dict);
        }

        public static Data From(Data data, params Data[] values) {
            return Data.From(new Dictionary<Data, IList<Data>>() {
                { data, values.ToList() }
            });
        }

        public static Data From(Data data, IList<Data> values) {
            return Data.From(new Dictionary<Data, IList<Data>>() {
                { data, values }
            });
        }

        public static implicit operator Data(string str) {
            return Data.From(str);
        }

        public static implicit operator Data(int value) {
            return Data.From(value);
        }

        public static bool operator ==(Data data1, Data data2) {
            return data1.Equals(data2);
        }

        public static bool operator !=(Data data1, Data data2) {
            return !data1.Equals(data2);
        }

        public abstract DataKind Kind { get; }

        public abstract bool IsTruthy { get; }

        private Data() { }

        public string AsSymbol() {
            return ((SymbolData)this).SymbolName;
        }

        public int AsInteger() {
            return ((IntegerData)this).Value;
        }

        public override bool Equals(object obj) {
            return base.Equals(obj);
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public IDictionary<Data, IList<Data>> AsDictionary() {
            return ((DictionaryData)this).Dictionary;
        }

        private class SymbolData : Data {
            public string SymbolName { get; }

            public override DataKind Kind => DataKind.Symbol;

            public override bool IsTruthy => true;

            public SymbolData(string name) {
                this.SymbolName = name;
            }

            public override string ToString() {
                return this.SymbolName;
            }

            public override bool Equals(object other) {
                if (other is SymbolData data) {
                    return data.SymbolName == this.SymbolName;
                }

                return false;
            }

            public override int GetHashCode() {
                return this.SymbolName.GetHashCode();
            }
        }

        private class IntegerData : Data {
            public int Value { get; }

            public override DataKind Kind => DataKind.Integer;

            public override bool IsTruthy => this.Value != 0;

            public IntegerData(int value) {
                this.Value = value;
            }

            public override string ToString() {
                return this.Value.ToString();
            }

            public override bool Equals(object other) {
                if (other is IntegerData data) {
                    return data.Value == this.Value;
                }

                return false;
            }

            public override int GetHashCode() {
                return this.Value.GetHashCode();
            }
        }

        private class DictionaryData : Data {
            public IDictionary<Data, IList<Data>> Dictionary { get; }

            public override DataKind Kind => DataKind.Dictionary;

            public override bool IsTruthy => this.Dictionary.Count > 0;

            public DictionaryData(IDictionary<Data, IList<Data>> dict) {
                this.Dictionary = dict;
            }

            public override bool Equals(object obj) {
                if (!(obj is DictionaryData dict)) {
                    return false;
                }

                return ReferenceEquals(this.Dictionary, dict.Dictionary);
            }

            public override int GetHashCode() {
                return this.Dictionary.GetHashCode();
            }
        }
    }
}