using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt14 {
    public class Serializer {
        public string Serialize(Data data) {
            StringBuilder sb = new StringBuilder();

            foreach (var s in this.SerializeAny(data)) {
                sb.AppendLine(s);
            }

            return sb.ToString();
        }

        private string[] SerializeAny(Data data) {
            if (data.Kind == DataKind.Dictionary) {
                return this.SerializeDictionary(data.AsDictionary());
            }
            else if (data.Kind == DataKind.Integer) {
                return this.SerializeInteger(data.AsInteger());
            }
            else if (data.Kind == DataKind.Symbol) {
                return this.SerializeSymbol(data.AsSymbol());
            }
            else {
                throw new Exception();
            }
        }

        private string[] SerializeDictionary(IDictionary<Data, Data> dict) {
            List<string> list = new List<string> { "{" };

            foreach (var pair in dict) {
                var key = this.SerializeAny(pair.Key).Select(x => "   " + x).ToArray();
                string[] value = this.SerializeAny(pair.Value);

                key[key.Length - 1] = key[key.Length - 1] + " : " + value[0];
                list.AddRange(key.Concat(value.Skip(1).Select(x => "   " + x)));
            }

            list.Add("}");

            return list.ToArray();
        }

        private string[] SerializeInteger(long i) {
            return new[] { i.ToString() };
        }

        private string[] SerializeSymbol(string s) {
            return new[] { s };
        }
    }
}