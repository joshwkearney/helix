using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt13 {
    public class Interpreter {
        private delegate Data Macro(IDictionary<Data, IList<Data>> data);

        private readonly IDictionary<ImmutableList<string>, Macro> macros =
            new Dictionary<ImmutableList<string>, Macro>(new ImmutableListComparer());

        private readonly Stack<Data> variables = new Stack<Data>();

        private readonly Stack<Data> funcs = new Stack<Data>();

        public Interpreter() {
            var scope = ImmutableDictionary<string, Data>.Empty;

            this.variables.Push(Data.From(new Dictionary<Data, IList<Data>>() {
                { "integer",  new[] { Data.From("value", "integer") }.ToList() },
                { "symbol",  new[] { Data.From("value", "symbol") }.ToList() },
                { "listinary",  new[] { Data.From("value", "listinary") }.ToList() }
            }));

            this.macros.Add(
                new[] { "define" }.ToImmutableList(),
                data => {
                    var definitions = data["define"][0].AsDictionary();
                    var newScope = this.variables.Peek().AsDictionary().ToDictionary(x => x.Key, x => x.Value);

                    foreach (var pair in definitions) {
                        newScope.Add(
                            pair.Key.AsSymbol(),
                            new[] { Data.From("value", this.Interpret(pair.Value[0])) }.ToList()
                        );
                    }

                    this.variables.Push(Data.From(newScope));
                    var result = this.Interpret(data["define"][1]);
                    this.variables.Pop();

                    return result;
                }
            );

            this.macros.Add(
                new[] { "if", "then", "else" }.ToImmutableList(),
                data => {
                    var condition = this.Interpret(data["if"][0]).IsTruthy;

                    if (condition) {
                        return this.Interpret(data["then"][0]);
                    }
                    else {
                        return this.Interpret(data["else"][0]);
                    }
                }
            );

            this.macros.Add(
                new[] { "typeof" }.ToImmutableList(),
                data => {
                    var arg = this.Interpret(data["typeof"][0]);

                    if (arg.Kind == DataKind.Dictionary) {
                        return "listinary";
                    }
                    else if (arg.Kind == DataKind.Integer) {
                        return "integer";
                    }
                    else if (arg.Kind == DataKind.Symbol) {
                        return "symbol";
                    }
                    else {
                        throw new Exception($"Unknown type '{arg.Kind}'");
                    }
                }
            );

            this.macros.Add(
                new[] { "count" }.ToImmutableList(),
                data => {
                    var arg = this.Interpret(data["count"][0]);

                    if (arg.Kind != DataKind.Dictionary) {
                        throw new Exception($"Cannot find the size of a value of type '{arg.Kind}'");
                    }

                    return arg.AsDictionary().Count;
                }
            );

            this.macros.Add(
                new[] { "function" }.ToImmutableList(),
                data => {
                    var list = data["function"];
                    var argNames = list.Take(list.Count - 1);
                    var body = list[list.Count - 1];

                    return Data.From(new Dictionary<Data, IList<Data>>() {
                        { "args", argNames.ToList() },
                        { "scope", new List<Data>() { this.variables.Peek() }},
                        { "body", new[] { body }.ToList() },
                    });
                }
            );

            this.macros.Add(
                new[] { "recurse" }.ToImmutableList(),
                data => {
                    var list = data["recurse"];
                    var result = this.Interpret(
                        Data.From(
                            Data.From("@", this.funcs.Peek()), 
                            list.Select(x => this.Interpret(x)).ToList()
                        )
                    );

                    return result;
                }
            );

            this.macros.Add(
                new[] { "get" }.ToImmutableList(),
                data => {
                    var (qualifiers, args) = this.SplitList(data["get"], "at");

                    if (qualifiers.Any(x => x.Kind == DataKind.Dictionary)) {
                        throw new Exception($"Cannot get variable: Invalid qualifiers");
                    }

                    int index = args.ContainsKey("at") ? args["at"][0].AsInteger() : 1;
                    index--;

                    if (qualifiers.Count == 0) {
                        throw new Exception("Cannot get variable: No qualifiers");
                    }
                    else if (qualifiers.Count == 1) {
                        if (index != 0) {
                            throw new Exception("Cannot get variable: Can't index raw value");
                        }

                        return this.Interpret(qualifiers[0]);
                    }
                    else {
                        var current = this.Interpret(qualifiers[0]);
                        foreach (var qualifier in qualifiers.Skip(1)) {
                            current = current.AsDictionary()[qualifier][index];
                        }

                        return current;
                    }
                }
            );

            this.macros.Add(
                new[] { "call" }.ToImmutableList(),
                data => {
                    var args = data["call"];
                    var qualifiers = args
                        .TakeWhile(x => x.Kind == DataKind.Symbol && x.AsSymbol() != "with")
                        .ToList();

                    if (!qualifiers.All(x => x.Kind == DataKind.Symbol)) {
                        throw new Exception($"Cannot get variable: Invalid qualifiers");
                    }

                    // Get function value
                    var funcValue = this.Interpret(qualifiers[0]);
                    foreach (var qualifier in qualifiers.Skip(1)) {
                        funcValue = funcValue.AsDictionary()[qualifier][0];
                    }

                    // No arguments
                    if (args.Count == qualifiers.Count) {
                        return this.Interpret(Data.From(Data.From("@", funcValue)));
                    }
                    else {
                        var argValues = args.Skip(qualifiers.Count + 1).ToList();
                        return this.Interpret(Data.From(Data.From("@", funcValue), argValues));
                    }
                }
            );

            this.macros.Add(
                new[] { "mist" }.ToImmutableList(),
                data => {
                    var dict = data["mist"][0].AsDictionary().ToDictionary(x => x.Key, x => x.Value);

                    foreach (var pair in dict.ToArray()) {
                        dict[pair.Key] = pair.Value.Select(x => this.Interpret(x)).ToList();
                    }

                    return Data.From(dict);
                }
            );

            this.macros.Add(
                new[] { "macro" }.ToImmutableList(),
                data => {
                    var list = data["macro"];
                    var keys = list.Take(list.Count - 2).Select(x => x.AsSymbol()).ToImmutableList();
                    var func = this.Interpret(list[list.Count - 2]);

                    this.macros.Add(
                        keys,
                        innerData => {
                            return this.Interpret(Data.From(
                                func, Data.From(innerData)    
                            ));
                        }
                    );

                    return this.Interpret(list[list.Count - 1]);
                }
            );

            this.macros.Add(
                new[] { "@" }.ToImmutableList(),
                data => {
                    var dict = data["@"][0].AsDictionary();

                    void process(IDictionary<Data, IList<Data>> input) {
                        foreach (var pair in input) {
                            for (int i = 0; i < pair.Value.Count; i++) {
                                if (pair.Value[i].Kind == DataKind.Dictionary) {
                                    var valueDict = pair.Value[i].AsDictionary();

                                    if (valueDict.Count == 1 && valueDict.Keys.First() == "$") {
                                        pair.Value[i] = this.Interpret(valueDict.Values.First()[0]);
                                    }

                                    process(valueDict);
                                }
                            }
                        }
                    }

                    process(dict);
                    return Data.From(dict);
                }
            );

            this.macros.Add(
                new[] { "+" }.ToImmutableList(),
                data => {
                    return data["+"]
                        .Select(x => this.Interpret(x).AsInteger())
                        .Aggregate((x, y) => x + y);                    
                }
            );

            this.macros.Add(
                new[] { "-" }.ToImmutableList(),
                data => {
                    return data["-"]
                        .Select(x => this.Interpret(x).AsInteger())
                        .Aggregate((x, y) => x - y);
                }
            );

            this.macros.Add(
                new[] { "*" }.ToImmutableList(),
                data => {
                    return data["*"]
                        .Select(x => this.Interpret(x).AsInteger())
                        .Aggregate((x, y) => x * y);
                }
            );

            this.macros.Add(
                new[] { "/" }.ToImmutableList(),
                data => {
                    return data["/"]
                        .Select(x => this.Interpret(x).AsInteger())
                        .Aggregate((x, y) => x / y);
                }
            );

            this.macros.Add(
                new[] { "<" }.ToImmutableList(),
                data => {
                    return data["<"]
                        .Select(x => this.Interpret(x))
                        .Aggregate((x, y) => Data.From(x.AsInteger() < y.AsInteger() ? 1 : 0));
                }
            );

            this.macros.Add(
                new[] { ">" }.ToImmutableList(),
                data => {
                    return data[">"]
                        .Select(x => this.Interpret(x))
                        .Aggregate((x, y) => Data.From(x.AsInteger() > y.AsInteger() ? 1 : 0));
                }
            );

            this.macros.Add(
                new[] { "=" }.ToImmutableList(),
                data => {
                    var args = data["="]
                        .Select(x => this.Interpret(x))
                        .ToList();
                        
                    if (args.Count == 1) {
                        return Data.From(1);
                    }
                    else {
                        var first = args.First();

                        foreach (var item in args.Skip(1)) {
                            if (first != item) {
                                return Data.From(0);
                            }
                        }

                        return Data.From(1);
                    }
                }
            );
        }

        public Data Interpret(Data data) {
            if (data.Kind == DataKind.Integer) {
                return data;
            }
            else if (data.Kind == DataKind.Symbol) {
                if (!variables.Peek().AsDictionary().TryGetValue(data.AsSymbol(), out var value)) {
                    throw new Exception();
                }

                return value[0].AsDictionary()["value"][0];
            }
            else if (data.Kind == DataKind.Dictionary) {
                var dict = data.AsDictionary();
                var keys = dict.Keys.Select(x => x).ToList();

                if (keys.All(x => x.Kind == DataKind.Symbol)) {
                    if (macros.TryGetValue(keys.Select(x => x.AsSymbol()).ToImmutableList(), out var macro)) { 
                        return macro(dict);
                    }
                }

                if (keys.Count == 1) {
                    var func = this.Interpret(keys[0]).AsDictionary();

                    if (!func.TryGetValue("args", out var argNames)) {
                        throw new Exception($"Invalid function object '{keys[0]}'");
                    }

                    if (!func.TryGetValue("body", out var body)) {
                        throw new Exception($"Invalid function object '{keys[0]}'");
                    }

                    if (!func.TryGetValue("scope", out var scope)) {
                        throw new Exception($"Invalid function object '{keys[0]}'");
                    }

                    var argValues = dict.Values.First().Select(x => this.Interpret(x)).ToList();
                    if (argNames.Count != argValues.Count) {
                        throw new Exception($"Cannot call function {keys[0]}: Parameter and argument counts do not match");
                    }

                    var invokeScope = scope[0].AsDictionary().ToDictionary(x => x.Key, x => x.Value);
                    foreach (var pair in argNames.Zip(argValues, (x, y) => new { Name = x.AsSymbol(), Value = y })) {
                        invokeScope.Add(pair.Name, new List<Data>() {
                            Data.From("value", pair.Value)
                        });
                    }

                    this.variables.Push(Data.From(invokeScope));
                    this.funcs.Push(Data.From(func));

                    var result = this.Interpret(body[0]);

                    this.variables.Pop();
                    this.funcs.Pop();

                    return result;
                }

                throw new Exception($"Cannot find macro or function '{string.Join("+", keys)}'");

            }
            else {
                throw new Exception();
            }
        }

        private (IList<Data> head, IDictionary<string, List<Data>> args) SplitList(IList<Data> list, params string[] keys) {
            var head = list.TakeWhile(x => keys.Any(y => y != x)).ToList();
            var args = new Dictionary<string, List<Data>>();

            if (head.Count < list.Count) {
                var currentList = new List<Data>();
                string currentKey = list[head.Count].AsSymbol();

                foreach (var item in list.Skip(head.Count + 1)) {
                    if (keys.Any(x => x == item)) {
                        args[currentKey] = currentList;
                        currentList = new List<Data>();
                        currentKey = item.AsSymbol();
                    }
                    else {
                        currentList.Add(item);
                    }
                }

                args[currentKey] = currentList;
            }

            return (head, args);
        }

        private class ImmutableListComparer : IEqualityComparer<ImmutableList<string>> {
            public bool Equals(ImmutableList<string> x, ImmutableList<string> y) {
                return x.SequenceEqual(y);
            }

            public int GetHashCode(ImmutableList<string> obj) {
                return obj.Aggregate(11, (x, y) => x + 37 * y.GetHashCode());
            }
        }
    }
}