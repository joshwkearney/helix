//using Attempt12.DataFormat;
//using System;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Linq;
//using System.Text;

//namespace Attempt12.Language {
//    public class LanguageInterpreter {
//        private Stack<Scope> scopes = new Stack<Scope>();
//        private Stack<Dictionary<Data, Data>> variables1 = new Stack<Dictionary<Data, Data>>();
//        private Stack<Data> currentFunction = new Stack<Data>();

//        public LanguageInterpreter() {
//            this.variables1.Push(new Dictionary<Data, Data>());
//            var scope = new Scope();

//            // "define" macro
//            scope = scope.SetMacro(
//                data => {
//                    var list = data["define"].AsList();
//                    var newVars = this.variables1.Peek().ToDictionary(x => x.Key, x => x.Value);

//                    foreach (var pair in list[0].AsDictionary()) {
//                        if (newVars.ContainsKey(pair.Key)) {
//                            throw new Exception($"Variable '{pair.Key.AsSymbol()}' is already defined");
//                        }

//                        newVars[pair.Key] = Data.FromDictionary(new Dictionary<Data, Data>() {
//                            { "value", this.Interpret(pair.Value) }
//                        });
//                    }

//                    this.variables1.Push(newVars);
//                    var result = this.Interpret(list[1]);
//                    this.variables1.Pop();

//                    return Data.FromList(new List<Data>() { "@", result });
//                },
//                "define"
//            );

//            // "if" macro
//            scope = scope.SetMacro(
//                data => {
//                    var cond = this.Interpret(data["if"]);

//                    if (cond.IsTruthy) {
//                        return data["then"];
//                    }
//                    else {
//                        return data["else"];
//                    }
//                },
//                "if", "then", "else"
//            );

//            // "+" macro
//            scope = scope.SetMacro(
//                data => {
//                    var list = data["+"].AsList();
//                    return list
//                        .Select(x => this.Interpret(x))
//                        .Select(x => x.AsInteger())
//                        .Aggregate((x, y) => x + y);
//                },
//                "+"
//            );

//            // "-" macro
//            scope = scope.SetMacro(
//                data => {
//                    var list = data["-"].AsList();
//                    return list
//                        .Select(x => this.Interpret(x))
//                        .Select(x => x.AsInteger())
//                        .Aggregate((x, y) => x - y);
//                },
//                "-"
//            );

//            // "*" macro
//            scope = scope.SetMacro(
//                data => {
//                    var list = data["*"].AsList();
//                    return list
//                        .Select(x => this.Interpret(x))
//                        .Select(x => x.AsInteger())
//                        .Aggregate((x, y) => x * y);
//                },
//                "*"
//            );

//            // "/" macro
//            scope = scope.SetMacro(
//                data => {
//                    var list = data["/"].AsList();
//                    return list
//                        .Select(x => this.Interpret(x))
//                        .Select(x => x.AsInteger())
//                        .Aggregate((x, y) => x / y);
//                },
//                "/"
//            );

//            // "greater" macro
//            scope = scope.SetMacro(
//                data => {
//                    var list = data["greater"].AsList();
//                    return list
//                        .Select(x => this.Interpret(x))
//                        .Select(x => x.AsInteger())
//                        .Aggregate((x, y) => x > y ? 1 : 0);
//                },
//                "greater"
//            );

//            // "less" macro
//            scope = scope.SetMacro(
//                data => {
//                    var list = data["less"].AsList();
//                    return list
//                        .Select(x => this.Interpret(x))
//                        .Select(x => x.AsInteger())
//                        .Aggregate((x, y) => x < y ? 1 : 0);
//                },
//                "less"
//            );

//            // "and" macro
//            scope = scope.SetMacro(
//                data => {
//                    var list = data["and"]
//                        .AsList()
//                        .Select(x => this.Interpret(x))
//                        .Aggregate((x, y) => (x.IsTruthy && y.IsTruthy) ? Data.FromInteger(1) : Data.FromInteger(0));

//                    return Data.FromDictionary(new Dictionary<Data, Data>() {
//                        { "@", list }
//                    });
//                },
//                "and"
//            );

//            // "or" macro
//            scope = scope.SetMacro(
//                data => {
//                    var list = data["or"]
//                        .AsList()
//                        .Select(x => this.Interpret(x))
//                        .Aggregate((x, y) => (x.IsTruthy || y.IsTruthy) ? Data.FromInteger(1) : Data.FromInteger(0));

//                    return Data.FromDictionary(new Dictionary<Data, Data>() {
//                        { "@", list }
//                    });
//                },
//                "or"
//            );

//            // "not" macro
//            scope = scope.SetMacro(
//                data => {                  
//                    return Data.FromList(new List<Data>() {
//                        "@", this.Interpret(data["not"].AsList()[0]).IsTruthy ? 0 : 1
//                    });
//                },
//                "not"
//            );

//            // list macro macro
//            scope = scope.SetMacro(
//                data => {
//                    var value = data["value"];
//                    var next = data["next"];

//                    if (!next.IsTruthy) {
//                        next = Data.FromDictionary(new Dictionary<Data, Data>());
//                    }

//                    return Data.FromDictionary(new Dictionary<Data, Data>() {
//                        { value, next }
//                    });
//                },
//                "value", "next"
//            );

//            // Translate a first:second:third format to a list
//            List<Data> getAccessors(Data access) {
//                if (access.Kind == DataKind.Dictionary) {
//                    var dict = access.AsDictionary();

//                    if (dict.Count != 1) {
//                        throw new Exception();
//                    }

//                    var list = getAccessors(dict.Values.First());
//                    list.Insert(0, dict.Keys.First());

//                    return list;
//                }
//                else {
//                    return new List<Data>() { access };
//                }
//            }

//            // "set" macro
//            scope = scope.SetMacro(
//                data => {                   
//                    var args = data["set"].AsList();
//                    if (args[1] != Data.FromSymbol("to")) {
//                        throw new Exception();
//                    }

//                    var accessors = getAccessors(args[0]);
//                    var value = this.Interpret(args[2]);

//                    if (accessors.Count == 0) {
//                        throw new Exception();
//                    }
//                    else if (accessors.Count == 1) {
//                        this.variables1.Peek()[accessors.First()].AsDictionary()["value"] = value;
//                    }
//                    else {
//                        var current = this.Interpret(accessors[0]).AsDictionary();

//                        foreach (var accessor in accessors.Skip(1).Take(accessors.Count - 2)) {
//                            if (current.ContainsKey(accessor)) {
//                                current = current[accessor].AsDictionary();
//                            }
//                            else {
//                                var empty = new Dictionary<Data, Data>();
//                                current[accessor] = Data.FromDictionary(empty);
//                                current = empty; 
//                            }
//                        }

//                        current[accessors[accessors.Count - 1]] = value;
//                    }

//                    return value;
//                },
//                "set"
//            );

//            // "get" macro
//            scope = scope.SetMacro(
//                data => {
//                    var arg = data["get"].AsList()[0];

//                    var accessors = getAccessors(arg);
//                    var current = this.Interpret(accessors[0]);

//                    foreach (var accessor in accessors.Skip(1)) {
//                        current = current.AsDictionary()[accessor];
//                    }

//                    return Data.FromList(new List<Data>() { "@", current });
//                },
//                "get"
//            );

//            // "function" macro
//            scope = scope.SetMacro(
//                data => {
//                    var list = data["function"].AsList();
//                    var args = list[0].AsList();
//                    var body = list[1];

//                    return Data.FromList(new List<Data>() {
//                        "@",
//                        Data.FromDictionary(new Dictionary<Data, Data>() {
//                            { "args", Data.FromList(args) },
//                            { "body", body },
//                            { "scope", Data.FromDictionary(this.variables1.Peek()) }
//                        })
//                    });
//                },
//                "function"
//            );

//            // "call" macro
//            scope = scope.SetMacro(
//                data => {
//                    var list = data["call"].AsList();
//                    var funcData = this.Interpret(list[0]);
//                    var func = funcData.AsDictionary();
//                    var argValues = list.Skip(1).Select(x => this.Interpret(x)).ToList();

//                    var argNames = func["args"].AsList();
//                    if (argNames.Count != argValues.Count) {
//                        throw new Exception();
//                    }

//                    var funcScope = func["scope"].AsDictionary().ToDictionary(x => x.Key, x => x.Value);
//                    foreach (var (name, value) in argNames.Zip(argValues, (x, y) => (name: x, value: y))) {
//                        funcScope[name] = Data.FromDictionary(new Dictionary<Data, Data>() {
//                            { "value", value }
//                        });
//                    }

//                    this.variables1.Push(funcScope);
//                    this.currentFunction.Push(funcData);

//                    var result = this.Interpret(func["body"]);

//                    this.variables1.Pop();
//                    this.currentFunction.Pop();

//                    return Data.FromList(new List<Data>() { "@", result });
//                },
//                "call"
//            );

//            // "recurse" macro
//            scope = scope.SetMacro(
//                data => {
//                    var code = new List<Data>() {
//                        "call",
//                        Data.FromList(new List<Data>() { "@", this.currentFunction.Peek() })
//                    };

//                    var list = data["recurse"].AsList();
//                    code.AddRange(list);

//                    var result = Data.FromList(code);

//                    return result;
//                },
//                "recurse"
//            );

//            // "macro" macro
//            scope = scope.SetMacro(
//                data => {
//                    var list = data["macro"].AsList();
//                    var args = list[0].AsDictionary();
//                    var names = args["keywords"].AsList().Select(x => x.AsSymbol()).ToArray();
//                    var func = this.Interpret(args["action"]);
//                    var appendix = list[1];

//                    var newScope = this.scopes.Peek();
//                    newScope = newScope.SetMacro(
//                        macroData => this.Interpret(
//                            Data.FromList(new List<Data>() {
//                                "call",
//                                Data.FromList(new List<Data>() { "@", func }),
//                                Data.FromList(new List<Data>() { "@", Data.FromDictionary(macroData) }),
//                            }
//                        )),
//                        names
//                    );

//                    this.scopes.Push(newScope);
//                    var result = this.Interpret(appendix);
//                    this.scopes.Pop();

//                    return Data.FromList(new List<Data>() {
//                        "@", result
//                    });
//                },
//                "macro"
//            );

//            // "eval" macro
//            scope = scope.SetMacro(
//                data => {
//                    var code = data["eval"].AsList()[0];
//                    return this.Interpret(code);
//                },
//                "eval"
//            );

//            // "recurse" macro
//            scope = scope.SetMacro(
//                data => {
//                    var list = data["ignore"].AsList();
//                    this.Interpret(list[0]);

//                    return list[1];
//                },
//                "ignore"
//            );

//            this.scopes.Push(scope);
//        }

//        public Data Interpret(Data code) {
//            switch (code.Kind) {
//                case DataKind.Symbol:
//                    if (this.variables1.Peek().TryGetValue(code, out var data)) {
//                        return data.AsDictionary()["value"];
//                    }
//                    else {
//                        throw new Exception($"Undefined symbol '{ code.AsSymbol() }'");
//                    }
//                case DataKind.Integer:
//                    return code;
//                case DataKind.Dictionary:
//                    return this.InterpretDictionary(code);
//                default:
//                    throw new Exception();
//            }
//        }

//        private Data InterpretDictionary(Data data) {
//            var dict = data.AsDictionary();

//            // Short circuit if this is a literal call
//            if (dict.Count == 1) {
//                var first = dict.First();

//                if (first.Key.Kind == DataKind.Symbol && dict.First().Key.AsSymbol() == "@") {
//                    var literal = first.Value.AsList()[0];
//                    this.AnalyzeLiteralData(literal);

//                    return literal;
//                }
//            }

//            // Resolve the correct macro
//            foreach (var macro in this.scopes.Peek().Macros) {
//                if (macro.Key.Count != dict.Count) {
//                    continue;
//                }

//                if (!macro.Key.SequenceEqual(dict.Keys.Where(x => x.Kind == DataKind.Symbol).Select(x => x.AsSymbol()))) {
//                    continue;
//                }

//                return this.Interpret(macro.Value(dict));
//            }

//            throw new Exception();
//        }

//        private void AnalyzeLiteralData(Data data) {
//            if (data.Kind == DataKind.Dictionary) {
//                var dictData = data.AsDictionary();

//                foreach (var pair in dictData.ToArray()) {
//                    if (pair.Value.Kind == DataKind.Dictionary) {
//                        var dictValue = pair.Value.AsDictionary();

//                        if (dictValue.Count == 1) {
//                            if (dictValue.Keys.First() == "$") {
//                                dictData[pair.Key] = this.Interpret(dictValue.Values.First());
//                            }
//                            else if (dictValue.Keys.First() == "@") {
//                                continue;
//                            }
//                        }
//                        else if (dictValue.Count == 2) {
//                            if (dictValue.TryGetValue("value", out var value) && dictValue.TryGetValue("next", out var next)) {
//                                if (value == "@") {
//                                    continue;
//                                }
//                                else if (value == "$") {
//                                    ;
//                                }
//                            }
//                        }
//                    }

//                    this.AnalyzeLiteralData(pair.Value);
//                }
//            }
//        }
//    }
//}