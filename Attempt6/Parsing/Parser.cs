using System;
using System.Collections.Generic;
using System.Linq;

namespace Attempt6.Parsing {
    public static class IntrinsicFunctions {
        public static Type FunctionType { get; } = typeof(Func<IReadOnlyList<object>, object>);

        public static object Add(IReadOnlyList<object> args) {
            return args.Aggregate((x, y) => (int)x + (int)y);
        }

        public static object Subtract(IReadOnlyList<object> args) {
            return args.Aggregate((x, y) => (int)x - (int)y);
        }

        public static object Multiply(IReadOnlyList<object> args) {
            return args.Aggregate((x, y) => (int)x * (int)y);
        }

        public static object Divide(IReadOnlyList<object> args) {
            return args.Aggregate((x, y) => (int)x / (int)y);
        }

        public static object Int32Equals(IReadOnlyList<object> args) {
            var ints = args.Select(x => (int)x);
            int first = ints.First();

            if (args.Count == 1) {
                return 1;
            }
            else {
                return ints.Skip(1).Aggregate(true, (x, y) => x && (y == first)) ? 1 : 0;
            }
        }

        public static IReadOnlyDictionary<string, Func<IReadOnlyList<object>, object>> Functions { get; } = new Dictionary<string, Func<IReadOnlyList<object>, object>>() {
            { "add", Add },
            { "subtract", Subtract },
            { "multipy", Multiply },
            { "divide", Divide },
            { "int32_equals", Int32Equals }
        };
    }

    public class Parser {
        private static ISyntax IfKeyword { get; } = new IdentifierSyntax("if");
        private static ISyntax ThenKeyword { get; } = new IdentifierSyntax("then");
        private static ISyntax ElseKeyword { get; } = new IdentifierSyntax("else");
        private static ISyntax LetKeyword { get; } = new IdentifierSyntax("let");
        private static ISyntax InKeyword { get; } = new IdentifierSyntax("in");
        private static ISyntax FunctionKeyword { get; } = new IdentifierSyntax("function");

        private readonly Stack<IReadOnlyDictionary<string, Type>> variableTypes = new Stack<IReadOnlyDictionary<string, Type>>();
        private readonly Dictionary<string, object> builtinScope = new Dictionary<string, object>();
        private readonly ISyntax tree;

        public Parser(ISyntax tree) {
            this.tree = tree;
        }

        public (IAST tree, IReadOnlyDictionary<string, object> builtinScope) Parse() {
            var scope = new Dictionary<string, Type> {
                { "recurse", IntrinsicFunctions.FunctionType }
            };

            foreach (var pair in IntrinsicFunctions.Functions) {
                scope.Add(pair.Key, IntrinsicFunctions.FunctionType);
                this.builtinScope.Add(pair.Key, pair.Value);
            }

            this.variableTypes.Push(scope);
            return (this.Parse(this.tree), this.builtinScope);
        }

        private IAST Parse(ISyntax tree) {
            if (tree is Int32Literal int32Atom) {
                return int32Atom;
            }
            else if (tree is IdentifierSyntax idAtom) {
                return new IdentifierLiteral(idAtom.Value, this.variableTypes.Peek()[idAtom.Value]);
            }
            else if (tree is ListSyntax syntax) {
                return this.Dict(syntax.List);
            }
            else {
                throw new Exception();
            }
        }

        private IAST Dict(AssociativeList<ISyntax, ISyntax> reader) {
            // (let x)
            if (reader.Key.Equals(LetKeyword)) {
                return this.LetExpr(reader);
            }
            // if: 
            else if (reader.Key.Equals(IfKeyword)) {
                return this.IfExpr(reader);
            }
            // function: x: add:(4 x)
            else if (reader.Key.Equals(FunctionKeyword)) {
                return this.FunctionExpr(reader);
            }
            else {
                // Here, this must be a function call in the form func:args

                var funcExpr = this.Parse(reader.Key);
                if (funcExpr.ReturnType == IntrinsicFunctions.FunctionType) {
                    var args = new ToListSyntaxVisitor(reader.Value)
                        .ToList()
                        .Select(x => this.Parse(x))
                        .ToList();

                    return new FunctionCallExpression(typeof(int), funcExpr, args);
                }
            }

            throw new Exception();
        }

        private IAST IfExpr(AssociativeList<ISyntax, ISyntax> reader) {
            var ifval = reader[IfKeyword];
            var thenval = reader[ThenKeyword];
            var elseval = reader[ElseKeyword];

            return new IfExpression(
                this.Parse(ifval),
                this.Parse(thenval),
                this.Parse(elseval)
            );
        }

        private IAST LetExpr(AssociativeList<ISyntax, ISyntax> reader) {
            // let: x: ...

            var letPair = (reader.Value as ListSyntax)?.List;
            if (letPair == null) {
                throw new Exception();
            }

            string name = (letPair.Key as IdentifierSyntax)?.Value;
            if (name == null) {
                throw new Exception();
            }

            var assign = this.Parse(letPair.Value);
            var nScope = this.variableTypes.Peek().ToDictionary(x => x.Key, x => x.Value);

            nScope.Add(name, assign.ReturnType);

            this.variableTypes.Push(nScope);
            var scope = this.Dict(reader.Next);
            this.variableTypes.Pop();

            return new LetExpression(
                name,
                assign,
                scope
            );
        }

        private IAST FunctionExpr(AssociativeList<ISyntax, ISyntax> reader) {
            // function: (x): add(4 x)
            var rest = (reader.Value as ListSyntax)?.List;
            if (rest == null || rest.Count != 1) {
                throw new Exception();
            }

            var argsSyntax = rest.First().Key;
            var bodySyntax = rest.First().Value;

            var args = new ToListSyntaxVisitor(argsSyntax)
                .ToList()
                .Select(x => {
                    if (x is IdentifierSyntax syntax) {
                        return syntax.Value;
                    }
                    else {
                        throw new Exception();
                    }
                })
                .ToList();

            var nScope = this.variableTypes.Peek().ToDictionary(x => x.Key, x => x.Value);

            foreach (string str in args) {
                nScope.Add(str, typeof(int));
            }

            this.variableTypes.Push(nScope);
            var body = this.Parse(bodySyntax);
            this.variableTypes.Pop();

            return new FunctionDeclaration(args, body);
        }

        private class ToListSyntaxVisitor : ISyntaxVisitor {
            private ISyntax syntax;
            private List<ISyntax> result = new List<ISyntax>();

            public ToListSyntaxVisitor(ISyntax syntax) {
                this.syntax = syntax;
            }

            public IReadOnlyList<ISyntax> ToList() {
                this.syntax.Accept(this);
                return this.result;
            }

            public void Visit(Int32Literal syntax) {
                this.result.Add( syntax );
            }

            public void Visit(IdentifierSyntax syntax) {
                this.result.Add(syntax);
            }

            public void Visit(ListSyntax syntax) {
                if (syntax.List.Count != 1) {
                    throw new Exception();
                }

                this.result.Add(syntax);
                //syntax.List.First().Value.Accept(this);
            }
        }
    }
}