using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt14 {
    public class StackInterpreter {
        private delegate void Macro(IDictionary<Data, Data> data);

        private readonly Stack<IReadOnlyDictionary<ImmutableList<string>, Macro>> macros =
            new Stack<IReadOnlyDictionary<ImmutableList<string>, Macro>>();

        private readonly Stack<Data> variables = new Stack<Data>();
        private readonly Stack<Data> funcs = new Stack<Data>();

        private readonly Stack<Data> values = new Stack<Data>();
        private readonly Stack<Action> commands = new Stack<Action>();

        public StackInterpreter() {
            this.variables.Push(Data.FromDictionary(new Dictionary<Data, Data>()));
            var scope = new Dictionary<ImmutableList<string>, Macro>(new ImmutableListComparer());

            scope.Add(
                new[] { "define" }.ToImmutableList(),
                data => {
                    var args = data["define"].AsList();
                    var definitions = args[0].AsDictionary().ToArray();

                    var newScope = this.variables
                        .Peek()
                        .AsDictionary()
                        .ToDictionary(x => x.Key, x => x.Value);

                    this.Eval(definitions.Select(x => x.Value).ToArray(), results => {
                        for (int i = 0; i < definitions.Length; i++) {
                            var value = Data.FromPair(
                                "value",
                                results[i]
                            );

                            newScope.Add(definitions[i].Key, value);
                        }

                        this.variables.Push(Data.FromDictionary(newScope));
                        this.commands.Push(() => this.variables.Pop());
                        this.PushCommand(args[1]);
                    });                    
                }
            );

            scope.Add(
                new[] { "eval" }.ToImmutableList(),
                data => {
                    var list = data["eval"].AsList();
                    var code = Data.FromList(list.Prepend("get").ToList());                    

                    this.Eval(code, result => {
                        this.Eval(result, result2 => {
                            this.commands.Push(() => this.values.Push(result2));
                        });
                    });              
                }
            );

            scope.Add(
                new[] { "macro" }.ToImmutableList(),
                data => {
                    var list = data["macro"].AsList();
                    var keys = list[0].AsList().Select(x => x.AsSymbol()).ToImmutableList();
                    var appendix = list[2];

                    this.Eval(list[1], func => {
                        var macroScope = this.macros
                            .Peek()
                            .ToDictionary(x => x.Key, x => x.Value, new ImmutableListComparer());

                        macroScope[keys] = innerData => {
                            var code = Data.FromList(new List<Data>() {
                                "call",
                                Data.FromPair("@", func),
                                Data.FromPair("@", Data.FromDictionary(innerData))
                            });

                            this.Eval(code, result => {
                                this.PushCommand(result);
                            });
                        };

                        this.macros.Push(macroScope);
                        this.commands.Push(() => this.macros.Pop());
                        this.PushCommand(appendix);
                    });
                }
            );

            scope.Add(
                new[] { "ignore" }.ToImmutableList(),
                data => {
                    var list = data["ignore"].AsList();

                    this.Eval(list[0], _ => {
                        this.PushCommand(list[1]);
                    });
                }
            );

            scope.Add(
                new[] { "if", "then", "else" }.ToImmutableList(),
                data => {
                    this.Eval(data["if"], result => {
                        if (result.IsTruthy) {
                            this.PushCommand(data["then"]);
                        }
                        else {
                            this.PushCommand(data["else"]);
                        }
                    });
                }
            );

            scope.Add(
                new[] { "function" }.ToImmutableList(),
                data => {
                    var args = data["function"].AsList();
                    var argNames = args.Take(args.Count - 1);
                    var body = args[args.Count - 1];

                    var funcData = Data.FromDictionary(
                        new Dictionary<Data, Data>() {
                            { "function", Data.FromDictionary(new Dictionary<Data, Data>() {
                                { "params", Data.FromList(argNames.ToList()) },
                                { "scope", this.variables.Peek() },
                                { "body", body }
                            })}
                        });

                    this.commands.Push(() => this.values.Push(funcData));
                }
            );

            scope.Add(
                new[] { "call" }.ToImmutableList(),
                data => {
                    this.Eval(data["call"].AsList(), results => {
                        var func = results[0].AsDictionary()["function"].AsDictionary();
                        var argValues = results.Skip(1).ToList();

                        if (!func.TryGetValue("params", out var argNamesData)) {
                            throw new Exception($"Invalid function object");
                        }

                        if (!func.TryGetValue("body", out var body)) {
                            throw new Exception($"Invalid function object");
                        }

                        if (!func.TryGetValue("scope", out var funcScope)) {
                            throw new Exception($"Invalid function object");
                        }

                        var argNames = argNamesData.AsList();
                        if (argNames.Count != argValues.Count) {
                            throw new Exception($"Cannot call function: Parameter and argument counts do not match");
                        }

                        var invokeScope = funcScope.AsDictionary().ToDictionary(x => x.Key, x => x.Value);
                        foreach (var pair in argNames.Zip(argValues, (x, y) => new { Name = x.AsSymbol(), Value = y })) {
                            invokeScope[pair.Name] = Data.FromPair("value", pair.Value);
                        }

                        if (this.variables.Peek().AsDictionary() != invokeScope) {
                            this.variables.Push(Data.FromDictionary(invokeScope));
                            this.commands.Push(() => this.variables.Pop());
                        }

                        if (this.funcs.Count == 0 || this.funcs.Peek() != results[0]) {
                            this.funcs.Push(results[0]);
                            this.commands.Push(() => this.funcs.Pop());
                        }

                        this.PushCommand(body);
                    });
                }
            );

            scope.Add(
                new[] { "recurse" }.ToImmutableList(),
                data => {
                    var code = Data.FromPair(
                        "call",
                        Data.FromList(
                            data["recurse"]
                                .AsList()
                                .Prepend(Data.FromPair("@", this.funcs.Peek()))
                                .ToList()
                        )
                    );

                    this.PushCommand(code);
                }
            );

            scope.Add(
                new[] { "get" }.ToImmutableList(),
                data => {
                    var arg = data["get"];

                    if (arg.Kind == DataKind.Integer || arg.Kind == DataKind.Symbol) {
                        this.PushCommand(arg);
                    }
                    else {
                        var list = arg.AsList();

                        for (int i = 1; i < list.Count; i++) {
                            if (list[i].Kind == DataKind.Symbol) {
                                list[i] = Data.FromPair("@", list[i]);
                            }
                        }

                        this.Eval(list, results => {
                            var current = results[0];

                            foreach (var accessor in results.Skip(1)) {
                                current = current.AsDictionary()[accessor];
                            }

                            this.commands.Push(() => this.values.Push(current));
                        });
                    }
                }
            );

            scope.Add(
                new[] { "set" }.ToImmutableList(),
                data => {
                    var args = data["set"].AsList();
                    args.Remove("to");

                    if (args.Count < 2) {
                        throw new Exception();
                    }
                    if (args.Count == 2) {
                        this.Eval(args[1], result => {
                            var vars = this.variables.Peek().AsDictionary();
                            vars[args[0]].AsDictionary()["value"] = result;

                            this.commands.Push(() => this.values.Push(result));
                        });
                    }
                    else {
                        for (int i = 1; i < args.Count - 1; i++) {
                            if (args[i].Kind == DataKind.Symbol) {
                                args[i] = Data.FromPair("@", args[i]);
                            }
                        }

                        this.Eval(args, results => {
                            var current = results[0];

                            foreach (var accessor in results.Skip(1).Take(results.Count - 2)) {
                                current = current.AsDictionary()[accessor];
                            }

                            current.AsDictionary()[results[results.Count - 2]] = results[results.Count - 1];
                            this.commands.Push(() => this.values.Push(results[results.Count - 2]));
                        });
                    }                    
                }
            );

            scope.Add(
                new[] { "count" }.ToImmutableList(),
                data => {
                    var code = Data.FromPair("get", data["count"]);

                    this.Eval(code, result => {
                        if (result.Kind == DataKind.Dictionary) {
                            this.PushCommand(result.AsDictionary().Count);
                        }
                        else {
                            this.PushCommand(1);
                        }
                    });                    
                }
            );

            scope.Add(
                new[] { "@" }.ToImmutableList(),
                data => {
                    this.commands.Push(() => this.values.Push(data["@"]));
                }
            );

            scope.Add(
                new[] { "$" }.ToImmutableList(),
                data => {
                    var code = data["$"].Clone();

                    void traverse(Data repData) {
                        if (repData.Kind == DataKind.Dictionary) {
                            var repDict = repData.AsDictionary();

                            foreach (var pair in repDict.ToArray()) {
                                if (pair.Value.Kind == DataKind.Dictionary) {
                                    var valueDict = pair.Value.AsDictionary();

                                    if (valueDict.Count == 1 && valueDict.Keys.First() == "$eval") {
                                        this.Eval(valueDict.Values.First(), result => {
                                            repDict[pair.Key] = result;
                                        });

                                        continue;
                                    }
                                }

                                traverse(pair.Value);
                            }
                        }
                    }

                    this.commands.Push(() => this.values.Push(code));
                    traverse(code);
                }
            );

            scope.Add(
                new[] { "*" }.ToImmutableList(),
                data => {
                    this.Eval(data["*"].AsList(), results => {
                        var sum = results
                            .Select(x => x.AsInteger())
                            .Aggregate((x, y) => x * y);

                        this.PushCommand(sum);
                    });
                }
            );

            scope.Add(
                new[] { "/" }.ToImmutableList(),
                data => {
                    this.Eval(data["/"].AsList(), results => {
                        var sum = results
                            .Select(x => x.AsInteger())
                            .Aggregate((x, y) => x / y);

                        this.PushCommand(sum);
                    });
                }
            );

            scope.Add(
                new[] { "+" }.ToImmutableList(),
                data => {
                    this.Eval(data["+"].AsList(), results => {
                        var sum = results
                            .Select(x => x.AsInteger())
                            .Aggregate((x, y) => x + y);

                        this.PushCommand(sum);
                    });
                }
            );

            scope.Add(
                new[] { "-" }.ToImmutableList(),
                data => {
                    this.Eval(data["-"].AsList(), results => {
                        var sum = results
                            .Select(x => x.AsInteger())
                            .Aggregate((x, y) => x - y);

                        this.PushCommand(sum);
                    });
                }
            );

            scope.Add(
                new[] { "<" }.ToImmutableList(),
                data => {
                    this.Eval(data["<"].AsList(), results => {
                        var sum = results
                            .Select(x => x.AsInteger())
                            .Aggregate((x, y) => x < y ? 1 : 0);

                        this.PushCommand(sum);
                    });
                }
            );

            scope.Add(
                new[] { ">" }.ToImmutableList(),
                data => {
                    this.Eval(data[">"].AsList(), results => {
                        var sum = results
                            .Select(x => x.AsInteger())
                            .Aggregate((x, y) => x > y ? 1 : 0);

                        this.PushCommand(sum);
                    });
                }
            );

            this.macros.Push(scope);
        }

        public Data Interpret(Data code) {
            this.values.Clear();
            this.commands.Clear();

            this.PushCommand(code);

            while (this.commands.Count > 0) {
                commands.Pop()();                
            }

            return this.values.Pop();
        }

        private void Eval(IList<Data> data, Action<IReadOnlyList<Data>> callback) {
            var count = data.Count;

            this.commands.Push(() => {
                List<Data> results = new List<Data>();
                for (int i = 0; i < count; i++) {
                    results.Add(values.Pop());
                }

                callback(results);
            });

            foreach (var item in data) {
                this.PushCommand(item);
            }
        }

        private void Eval(Data data, Action<Data> callback) {
            this.commands.Push(() => {                
                callback(this.values.Pop());
            });

            this.PushCommand(data);
        }

        private void PushCommand(Data code) {
            if (code.Kind == DataKind.Integer) {
                this.commands.Push(() => this.values.Push(code.AsInteger()));
            }
            else if (code.Kind == DataKind.Symbol) {
                if (!variables.Peek().AsDictionary().TryGetValue(code.AsSymbol(), out var value)) {
                    throw new Exception($"Cannot access undefined variable '{code}'");
                }

                this.commands.Push(() => this.values.Push(value.AsDictionary()["value"]));
            }
            else if (code.IsList()) {
                var list = code.AsList();
                var newCode = Data.FromPair(list[0], Data.FromList(list.Skip(1).ToList()));

                this.PushCommand(newCode);
            }
            else {
                var dict = code.AsDictionary();
                var keys = dict.Keys.Select(x => x.AsSymbol()).ToImmutableList();

                // Attempt to invoke macro
                if (this.macros.Peek().TryGetValue(keys, out var macro)) {
                    macro(dict);
                    return;
                }

                // Rewrite an implicit function invoke with a call macro
                if (keys.Count == 1 && dict.Values.First().IsList()) {
                    this.PushCommand(Data.FromPair(
                        "call",
                        Data.FromList(
                            dict
                                .Values
                                .First()
                                .AsList()
                                .Prepend(dict.Keys.First())
                                .ToList()
                        )
                    ));

                    return;
                }

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