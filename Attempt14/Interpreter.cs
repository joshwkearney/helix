using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt14 {
    public class Interpreter {
        private delegate Data Macro(IDictionary<Data, Data> data);

        private readonly IDictionary<ImmutableList<string>, Macro> macros =
            new Dictionary<ImmutableList<string>, Macro>(new ImmutableListComparer());

        private readonly Stack<Data> variables = new Stack<Data>();
        private readonly Stack<Data> funcs = new Stack<Data>();

        public Interpreter() {
            this.variables.Push(Data.FromDictionary(new Dictionary<Data, Data>()));

            this.macros.Add(
                new[] { "define" }.ToImmutableList(),
                data => {
                    var args = data["define"].AsList();
                    var definitions = args[0].AsDictionary();

                    var newScope = this.variables
                        .Peek()
                        .AsDictionary()
                        .ToDictionary(x => x.Key, x => x.Value);

                    foreach (var pair in definitions) {
                        var value = Data.FromPair(
                            "value",
                            this.Interpret(pair.Value)
                        );

                        newScope.Add(pair.Key, value);
                    }

                    this.variables.Push(Data.FromDictionary(newScope));
                    var result = this.Interpret(args[1]);
                    this.variables.Pop();

                    return result;
                }
            );

            this.macros.Add(
                new[] { "ignore" }.ToImmutableList(),
                data => {
                    var args = data["ignore"].AsList();

                    this.Interpret(args[0]);
                    return this.Interpret(args[1]);
                }
            );

            this.macros.Add(
                new[] { "if", "then", "else" }.ToImmutableList(),
                data => {
                    var condition = this.Interpret(data["if"]).IsTruthy;

                    if (condition) {
                        return this.Interpret(data["then"]);
                    }
                    else {
                        return this.Interpret(data["else"]);
                    }
                }
            );

            this.macros.Add(
                new[] { "count" }.ToImmutableList(),
                data => {
                    var value = this.Interpret(Data.FromPair("get", data["count"]));

                    if (value.Kind == DataKind.Dictionary) {
                        return value.AsDictionary().Count;
                    }        
                    else {
                        return 1;
                    }
                }
            );

            this.macros.Add(
                new[] { "get" }.ToImmutableList(),
                data => {
                    var arg = data["get"];

                    if (arg.Kind == DataKind.Integer) {
                        return arg;
                    }
                    else if (arg.Kind == DataKind.Symbol) {
                        return this.Interpret(arg);
                    }
                    else {
                        var list = arg.AsList();
                        var current = this.Interpret(list[0]);

                        foreach (var accessor in list.Skip(1)) {
                            if (accessor.Kind == DataKind.Dictionary) {
                                current = current.AsDictionary()[this.Interpret(accessor)];
                            }
                            else {
                                current = current.AsDictionary()[accessor];
                            }
                        }

                        return current;
                    }
                }
            );

            this.macros.Add(
                new[] { "set" }.ToImmutableList(),
                data => {
                    var list = data["set"].AsList();
                    var accessors = list.TakeWhile(x => x != "to").ToArray();
                    var value = this.Interpret(list[list.Count - 1]);

                    if (accessors.Length == 1) {
                        if (!this.variables.Peek().AsDictionary().TryGetValue(accessors[0], out var var)) {
                            throw new Exception($"Cannot set value to nonexistant variable '{accessors[0]}'");
                        }

                        var.AsDictionary()["value"] = value;
                    }
                    else {
                        var current = this.Interpret(accessors[0]).AsDictionary();

                        foreach (var accessor in accessors.Skip(1).Take(accessors.Length - 2)) {
                            Data next;
                            if (accessor.Kind == DataKind.Dictionary) {
                                next = current[this.Interpret(accessor)];
                            }
                            else {
                                next = current[accessor];
                            }

                            current = next.AsDictionary();
                        }

                        var last = accessors[accessors.Length - 1];
                        if (last.Kind == DataKind.Dictionary) {
                            last = this.Interpret(last);
                        }

                        current[last] = value;
                    }

                    return value;
                }
            );

            this.macros.Add(
                new[] { "function" }.ToImmutableList(),
                data => {
                    var args = data["function"].AsList();
                    var argNames = args.Take(args.Count - 1);
                    var body = args[args.Count - 1];

                    return Data.FromDictionary(
                        new Dictionary<Data, Data>() {
                            { "function", Data.FromDictionary(new Dictionary<Data, Data>() {
                                { "params", Data.FromList(argNames.ToList()) },
                                { "scope", this.variables.Peek() },
                                { "body", body }
                            })}
                        });
                }
            );

            this.macros.Add(
                new[] { "recurse" }.ToImmutableList(),
                data => {
                    var list = data["recurse"].AsList();
                    var result = this.Interpret(
                        Data.FromPair(
                            Data.FromPair("@", this.funcs.Peek()),
                            Data.FromList(list.Select(x => this.Interpret(x)).ToList())
                        )
                    );

                    return result;
                }
            );

            this.macros.Add(
                new[] { "map" }.ToImmutableList(),
                data => {
                    var arg = data["map"];
                    var result = new Dictionary<Data, Data>();
                    IDictionary<Data, Data> dict;

                    if (arg.IsList()) {
                        var list = arg.AsList();

                        if (list.Count != 1) {
                            throw new Exception("Cannot create map: invalid arguments");
                        }

                        dict = list[0].AsDictionary();
                    }
                    else {
                        dict = arg.AsDictionary();
                    }

                    foreach (var pair in dict) {
                        if (pair.Key.Kind == DataKind.Dictionary) {
                            throw new Exception("Map keys cannot be complex values");
                        }

                        result[pair.Key] = this.Interpret(pair.Value);
                    }

                    return Data.FromDictionary(result);
                }
            );

            this.macros.Add(
                new[] { "+" }.ToImmutableList(),
                data => {
                    return data["+"]
                        .AsList()
                        .Select(x => this.Interpret(x).AsInteger())
                        .Aggregate((x, y) => x + y);
                }
            );

            this.macros.Add(
                new[] { "-" }.ToImmutableList(),
                data => {
                    return data["-"]
                        .AsList()
                        .Select(x => this.Interpret(x).AsInteger())
                        .Aggregate((x, y) => x - y);
                }
            );

            this.macros.Add(
                new[] { "*" }.ToImmutableList(),
                data => {
                    return data["*"]
                        .AsList()
                        .Select(x => this.Interpret(x).AsInteger())
                        .Aggregate((x, y) => x * y);
                }
            );

            this.macros.Add(
                new[] { "/" }.ToImmutableList(),
                data => {
                    return data["/"]
                        .AsList()
                        .Select(x => this.Interpret(x).AsInteger())
                        .Aggregate((x, y) => x / y);
                }
            );

            this.macros.Add(
                new[] { "<" }.ToImmutableList(),
                data => {
                    return data["<"]    
                        .AsList()
                        .Select(x => this.Interpret(x))
                        .Aggregate((x, y) => Data.FromInteger(x.AsInteger() < y.AsInteger() ? 1 : 0));
                }
            );

            this.macros.Add(
                new[] { ">" }.ToImmutableList(),
                data => {
                    return data[">"]
                        .AsList()
                        .Select(x => this.Interpret(x))
                        .Aggregate((x, y) => Data.FromInteger(x.AsInteger() > y.AsInteger() ? 1 : 0));
                }
            );

            this.macros.Add(
                new[] { "=" }.ToImmutableList(),
                data => {
                    var args = data["="]
                        .AsList()
                        .Select(x => this.Interpret(x))
                        .ToList();

                    if (args.Count == 1) {
                        return Data.FromInteger(1);
                    }
                    else {
                        var first = args.First();

                        foreach (var item in args.Skip(1)) {
                            if (first != item) {
                                return Data.FromInteger(0);
                            }
                        }

                        return Data.FromInteger(1);
                    }
                }
            );

            this.macros.Add(
                new[] { "@" }.ToImmutableList(),
                data => {
                    return data["@"];
                }
            );
        }

        public Data Interpret(Data data) {
            if (data.Kind == DataKind.Integer) {
                return data;
            }
            else if (data.Kind == DataKind.Symbol) {
                if (!variables.Peek().AsDictionary().TryGetValue(data.AsSymbol(), out var value)) {
                    throw new Exception($"Cannot access undefined variable '{data}'");
                }

                return value.AsDictionary()["value"];
            }
            else if (data.Kind == DataKind.Dictionary) {
                var dict = data.AsDictionary();
                var keys = dict.Keys.Select(x => x).ToList();

                // Invoke macro
                if (keys.All(x => x.Kind == DataKind.Symbol)) {
                    if (macros.TryGetValue(keys.Select(x => x.AsSymbol()).ToImmutableList(), out var macro)) { 
                        return macro(dict);
                    }
                }

                // Invoke function
                if (keys.Count == 1) {
                    var funcContainer = this.Interpret(keys.First()).AsDictionary();
                    var func = funcContainer["function"].AsDictionary();
                    var argValues = dict.Values.First().AsList().Select(x => this.Interpret(x)).ToList();

                    if (!func.TryGetValue("params", out var argNamesData)) {
                        throw new Exception($"Invalid function object '{keys[0]}'");
                    }

                    if (!func.TryGetValue("body", out var body)) {
                        throw new Exception($"Invalid function object '{keys[0]}'");
                    }

                    if (!func.TryGetValue("scope", out var scope)) {
                        throw new Exception($"Invalid function object '{keys[0]}'");
                    }

                    var argNames = argNamesData.AsList();
                    if (argNames.Count != argValues.Count) {
                        throw new Exception($"Cannot call function {keys[0]}: Parameter and argument counts do not match");
                    }

                    var invokeScope = scope.AsDictionary().ToDictionary(x => x.Key, x => x.Value);
                    foreach (var pair in argNames.Zip(argValues, (x, y) => new { Name = x.AsSymbol(), Value = y })) {
                        invokeScope.Add(
                            pair.Name, 
                            Data.FromPair("value", pair.Value)
                        );
                    }

                    this.variables.Push(Data.FromDictionary(invokeScope));
                    this.funcs.Push(Data.FromDictionary(funcContainer));

                    var result = this.Interpret(body);

                    this.variables.Pop();
                    this.funcs.Pop();

                    return result;
                }

                // Rewrite lists to key/value pairs
                if (keys.All(x => x.Kind == DataKind.Integer)) {
                    var list = data.AsList();

                    return this.Interpret(Data.FromPair(list[0], Data.FromList(list.Skip(1).ToList())));
                }

                throw new Exception($"Cannot find macro or function '{string.Join("+", keys)}'");
            }
            else {
                throw new Exception();
            }
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