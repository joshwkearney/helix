using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt12.DataFormat {
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

        public static Data From(IDictionary<Data, Data> dict) {
            return new DictionaryData(
                dict, 
                DataKind.Dictionary);
        }

        public static Data From(IList<Data> list) {
            if (list.Count == 0) {
                return Data.From(new Dictionary<Data, Data>());
            }
            else if (list.Count == 1) {
                return Data.From(new Dictionary<Data, Data>() {
                    { "value", list[0] },
                    { "next", Data.From(0) }
                });
            }
            else {
                return list
                    .Reverse()
                    .Aggregate(
                        Data.From(0), 
                        (x, y) => From(new Dictionary<Data, Data>() {
                            { "value", y },
                            { "next", x }
                        }));
            }
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

        public IDictionary<Data, Data> AsDictionary() {
            return ((DictionaryData)this).Dictionary;
        } 

        public IList<Data> AsList() {
            List<Data> list = new List<Data>();
            var current = this.AsDictionary();

            if (current.Count == 0) {
                return list;
            }

            while (current["next"].IsTruthy) {
                list.Add(current["value"]);
                current = current["next"].AsDictionary();
            }

            list.Add(current["value"]);

            return list;
        }

        public bool IsList() {
            if (!(this is DictionaryData dict)) {
                return false;
            }

            return dict.Dictionary.ContainsKey("value") && 
                dict.Dictionary.ContainsKey("next") &&
                dict.Dictionary["next"].IsList();
        }

        private class SymbolData : Data{
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
            public IDictionary<Data, Data> Dictionary { get; }

            public override DataKind Kind { get; }

            public override bool IsTruthy => this.Dictionary.Count > 0;

            public DictionaryData(IDictionary<Data, Data> dict, DataKind kind) {
                this.Dictionary = dict;
                this.Kind = kind;
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