using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt14 {
    public enum DataKind {
        Symbol, Integer, Dictionary
    }

    public abstract class Data {
        public static Data FromInteger(long value) {
            return new LongData(value);
        }

        public static Data FromSymbol(string symbol) {
            return new SymbolData(symbol);
        }

        public static Data FromDictionary(IDictionary<Data, Data> dict) {
            return new DictionaryData(
                dict, 
                DataKind.Dictionary);
        }

        public static Data FromPair(Data key, Data value) {
            return Data.FromDictionary(new Dictionary<Data, Data>() {
                { key, value }
            });
        }

        public static Data FromList(IList<Data> list) {
            if (list.Count == 0) {
                return Data.FromDictionary(new Dictionary<Data, Data>());
            }
            else {
                Dictionary<Data, Data> dict = new Dictionary<Data, Data>();

                for (int i = 0; i < list.Count; i++) {
                    dict[Data.FromInteger(i + 1)] = list[i];
                }

                return Data.FromDictionary(dict);
            }
        }

        public static Data FromList(params Data[] list) {
            return FromList(list.ToList());
        }

        public static implicit operator Data(string str) {
            return Data.FromSymbol(str);
        }

        public static implicit operator Data(int value) {
            return Data.FromInteger(value);
        }

        public static implicit operator Data(long value) {
            return Data.FromInteger(value);
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

        public abstract Data Clone();

        public string AsSymbol() {
            return ((SymbolData)this).SymbolName;
        }

        public long AsInteger() {
            return ((LongData)this).Value;
        }

        public IDictionary<Data, Data> AsDictionary() {
            return ((DictionaryData)this).Dictionary;
        } 

        public IList<Data> AsList() {
            var dict = this.AsDictionary();
            var list = new List<Data>();
            
            for (int i = 1; i <= dict.Count; i++) {
                list.Add(dict[i]);
            }

            return list;
        }

        public bool IsList() {
            if (!(this is DictionaryData dict)) {
                return false;
            }

            for (int i = 1; i <= dict.Dictionary.Count; i++) {
                if (!dict.Dictionary.ContainsKey(i)) {
                    return false;
                }
            }

            return true;
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

            public override Data Clone() {
                return this;
            }
        }

        private class LongData : Data {
            public long Value { get; }

            public override DataKind Kind => DataKind.Integer;

            public override bool IsTruthy => this.Value != 0;

            public LongData(long value) {
                this.Value = value;
            }

            public override string ToString() {
                return this.Value.ToString();
            }

            public override bool Equals(object other) {
                if (other is LongData data) {
                    return data.Value == this.Value;
                }

                return false;
            }

            public override int GetHashCode() {
                return this.Value.GetHashCode();
            }

            public override Data Clone() {
                return this;
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

            public override Data Clone() {
                var dict = new Dictionary<Data, Data>();

                foreach (var pair in this.Dictionary) {
                    dict[pair.Key.Clone()] = pair.Value.Clone();
                }

                return Data.FromDictionary(dict);
            }
        }
    }    
}