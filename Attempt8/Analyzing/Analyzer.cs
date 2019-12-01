using Attempt12.Interpreting;
using Attempt12.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt12.Analyzing {
    public delegate ISyntax Analyzable(Analyzer analyzer);

    public class AnalyzeResult {
        public ISyntax SyntaxTree { get; }

        public InterprativeScope InitialScope { get; }

        public AnalyzeResult(ISyntax syntaxTree, InterprativeScope initialScope) {
            this.SyntaxTree = syntaxTree;
            this.InitialScope = initialScope;
        }
    }

    public enum BinaryOperator {
        Add, Subtract, Multiply, Divide
    }

    public class Analyzer {
        private readonly Stack<AnalyticScope> scopes = new Stack<AnalyticScope>();
        private readonly Analyzable parseResult;

        public Analyzer(Analyzable parseResult) {
            this.parseResult = parseResult;
        }

        public AnalyzeResult Analyze() {
            this.scopes.Clear();

            var scope = new AnalyticScope();
            scope = scope.AddType("int32", PrimitiveTypes.Int32Type);
            scope = scope.AddType("float32", PrimitiveTypes.Float32Type);
            this.scopes.Push(scope);

            var int32FunctionType = new FunctionTypeSymbol(
                PrimitiveTypes.Int32Type,
                PrimitiveTypes.Int32Type,
                PrimitiveTypes.Int32Type
            );

            var float32FunctionType = new FunctionTypeSymbol(
                PrimitiveTypes.Float32Type,
                PrimitiveTypes.Float32Type,
                PrimitiveTypes.Float32Type
            );

            var addInt32Location = new VariableLocation("Add", int32FunctionType);
            var subtractInt32Location = new VariableLocation("Subtract", int32FunctionType);
            var multiplyInt32Location = new VariableLocation("Multiply", int32FunctionType);
            var divideInt32Location = new VariableLocation("Divide", int32FunctionType);

            var addFloat32Location = new VariableLocation("Add", float32FunctionType);
            var subtractFloat32Location = new VariableLocation("Subtract", float32FunctionType);
            var multiplyFloat32Location = new VariableLocation("Multiply", float32FunctionType);
            var divideFloat32Location = new VariableLocation("Divide", float32FunctionType);

            scope = scope.AddStaticMember(PrimitiveTypes.Int32Type, "Add", addInt32Location);
            scope = scope.AddStaticMember(PrimitiveTypes.Int32Type, "Subtract", subtractInt32Location);
            scope = scope.AddStaticMember(PrimitiveTypes.Int32Type, "Multiply", multiplyInt32Location);
            scope = scope.AddStaticMember(PrimitiveTypes.Int32Type, "Divide", divideInt32Location);

            scope = scope.AddStaticMember(PrimitiveTypes.Float32Type, "Add", addFloat32Location);
            scope = scope.AddStaticMember(PrimitiveTypes.Float32Type, "Subtract", subtractFloat32Location);
            scope = scope.AddStaticMember(PrimitiveTypes.Float32Type, "Multiply", multiplyFloat32Location);
            scope = scope.AddStaticMember(PrimitiveTypes.Float32Type, "Divide", divideFloat32Location);

            this.scopes.Push(scope);

            var addInt32 = this.EnsureScopeCount(this.scopes.Count(), () => {
                return new Closure(
                    this.AnalyzeFunctionDefinition(
                        new (TypeResolvable, string)[] {
                            ((TypeResolver x) => x.ResolveTypeIdentifier("int32"), "x"),
                            ((TypeResolver x) => x.ResolveTypeIdentifier("int32"), "y"),
                        },
                        analyzer => analyzer.AnalyzeIntrinsic(
                            IntrinsicDescriptor.AddInt32,
                            new Analyzable[] {
                                analyzer2 => analyzer2.AnalyzeVariableReference("x"),
                                analyzer2 => analyzer2.AnalyzeVariableReference("y"),
                            }
                        )
                    ) as FunctionDefinitionSyntax,
                    new InterprativeScope()
                );
            });
            var subtractInt32 = this.EnsureScopeCount(this.scopes.Count(), () => {
                return new Closure(
                    this.AnalyzeFunctionDefinition(
                        new(TypeResolvable, string)[] {
                            ((TypeResolver x) => x.ResolveTypeIdentifier("int32"), "x"),
                            ((TypeResolver x) => x.ResolveTypeIdentifier("int32"), "y"),
                        },
                        analyzer => analyzer.AnalyzeIntrinsic(
                            IntrinsicDescriptor.SubtractInt32,
                            new Analyzable[] {
                                analyzer2 => analyzer2.AnalyzeVariableReference("x"),
                                analyzer2 => analyzer2.AnalyzeVariableReference("y"),
                            }
                        )
                    ) as FunctionDefinitionSyntax,
                    new InterprativeScope()
                );
            });
            var multiplyInt32 = this.EnsureScopeCount(this.scopes.Count(), () => {
                return new Closure(
                    this.AnalyzeFunctionDefinition(
                        new(TypeResolvable, string)[] {
                            ((TypeResolver x) => x.ResolveTypeIdentifier("int32"), "x"),
                            ((TypeResolver x) => x.ResolveTypeIdentifier("int32"), "y"),
                        },
                        analyzer => analyzer.AnalyzeIntrinsic(
                            IntrinsicDescriptor.MultiplyInt32,
                            new Analyzable[] {
                                analyzer2 => analyzer2.AnalyzeVariableReference("x"),
                                analyzer2 => analyzer2.AnalyzeVariableReference("y"),
                            }
                        )
                    ) as FunctionDefinitionSyntax,
                    new InterprativeScope()
                );
            });
            var divideInt32 = this.EnsureScopeCount(this.scopes.Count(), () => {
                return new Closure(
                    this.AnalyzeFunctionDefinition(
                        new(TypeResolvable, string)[] {
                            ((TypeResolver x) => x.ResolveTypeIdentifier("int32"), "x"),
                            ((TypeResolver x) => x.ResolveTypeIdentifier("int32"), "y"),
                        },
                        analyzer => analyzer.AnalyzeIntrinsic(
                            IntrinsicDescriptor.DivideInt32,
                            new Analyzable[] {
                                analyzer2 => analyzer2.AnalyzeVariableReference("x"),
                                analyzer2 => analyzer2.AnalyzeVariableReference("y"),
                            }
                        )
                    ) as FunctionDefinitionSyntax,
                    new InterprativeScope()
                );
            });

            var addReal32 = this.EnsureScopeCount(this.scopes.Count(), () => {
                return new Closure(
                    this.AnalyzeFunctionDefinition(
                        new(TypeResolvable, string)[] {
                            ((TypeResolver x) => x.ResolveTypeIdentifier("float32"), "x"),
                            ((TypeResolver x) => x.ResolveTypeIdentifier("float32"), "y"),
                        },
                        analyzer => analyzer.AnalyzeIntrinsic(
                            IntrinsicDescriptor.AddReal32,
                            new Analyzable[] {
                                analyzer2 => analyzer2.AnalyzeVariableReference("x"),
                                analyzer2 => analyzer2.AnalyzeVariableReference("y"),
                            }
                        )
                    ) as FunctionDefinitionSyntax,
                    new InterprativeScope()
                );
            });
            var subtractReal32 = this.EnsureScopeCount(this.scopes.Count(), () => {
                return new Closure(
                    this.AnalyzeFunctionDefinition(
                        new(TypeResolvable, string)[] {
                            ((TypeResolver x) => x.ResolveTypeIdentifier("float32"), "x"),
                            ((TypeResolver x) => x.ResolveTypeIdentifier("float32"), "y"),
                        },
                        analyzer => analyzer.AnalyzeIntrinsic(
                            IntrinsicDescriptor.SubtractReal32,
                            new Analyzable[] {
                                analyzer2 => analyzer2.AnalyzeVariableReference("x"),
                                analyzer2 => analyzer2.AnalyzeVariableReference("y"),
                            }
                        )
                    ) as FunctionDefinitionSyntax,
                    new InterprativeScope()
                );
            });
            var multiplyReal32 = this.EnsureScopeCount(this.scopes.Count(), () => {
                return new Closure(
                    this.AnalyzeFunctionDefinition(
                        new(TypeResolvable, string)[] {
                            ((TypeResolver x) => x.ResolveTypeIdentifier("float32"), "x"),
                            ((TypeResolver x) => x.ResolveTypeIdentifier("float32"), "y"),
                        },
                        analyzer => analyzer.AnalyzeIntrinsic(
                            IntrinsicDescriptor.MultiplyReal32,
                            new Analyzable[] {
                                analyzer2 => analyzer2.AnalyzeVariableReference("x"),
                                analyzer2 => analyzer2.AnalyzeVariableReference("y"),
                            }
                        )
                    ) as FunctionDefinitionSyntax,
                    new InterprativeScope()
                );
            });
            var divideReal32 = this.EnsureScopeCount(this.scopes.Count(), () => {
                return new Closure(
                    this.AnalyzeFunctionDefinition(
                        new(TypeResolvable, string)[] {
                            ((TypeResolver x) => x.ResolveTypeIdentifier("float32"), "x"),
                            ((TypeResolver x) => x.ResolveTypeIdentifier("float32"), "y"),
                        },
                        analyzer => analyzer.AnalyzeIntrinsic(
                            IntrinsicDescriptor.DivideReal32,
                            new Analyzable[] {
                                analyzer2 => analyzer2.AnalyzeVariableReference("x"),
                                analyzer2 => analyzer2.AnalyzeVariableReference("y"),
                            }
                        )
                    ) as FunctionDefinitionSyntax,
                    new InterprativeScope()
                );
            });

            var initialScope = new InterprativeScope();

            initialScope = initialScope.AddVariable(addInt32Location, addInt32);
            initialScope = initialScope.AddVariable(subtractInt32Location, subtractInt32);
            initialScope = initialScope.AddVariable(multiplyInt32Location, multiplyInt32);
            initialScope = initialScope.AddVariable(divideInt32Location, divideInt32);

            initialScope = initialScope.AddVariable(addFloat32Location, addReal32);
            initialScope = initialScope.AddVariable(subtractFloat32Location, subtractReal32);
            initialScope = initialScope.AddVariable(multiplyFloat32Location, multiplyReal32);
            initialScope = initialScope.AddVariable(divideFloat32Location, divideReal32);

            return new AnalyzeResult(this.parseResult(this), initialScope);
        }

        public ISyntax AnalyzeInt32(int value) {
            return new Int32Syntax(value, this.scopes.Peek());
        }

        public ISyntax AnalyzeReal32(float value) {
            return new Real32Syntax(value, this.scopes.Peek());
        }

        public ISyntax AnalyzeVariableReference(string name) {
            if (!this.scopes.Peek().Variables.TryGetValue(name, out var variableLocation)) {
                throw new Exception($"Use of undefined variable '{name}'");
            }

            return new VariableReferenceSyntax(variableLocation, this.scopes.Peek());
        }

        public ISyntax AnalyzeVariableDeclaration(string name, Analyzable assignExpr) {
            var bodyResult = assignExpr(this);
            var variable = new VariableLocation(name, bodyResult.TypeSymbol, true);

            AnalyticScope scope = this.scopes.Peek().AddVariable(
                name,
                variable
            );
            this.scopes.Push(scope);

            return new VariableDeclarationSyntax(bodyResult, variable, this.scopes.Peek());
        }

        public ISyntax AnalyzeStatement(Analyzable first, Analyzable second) {
            int originalCount = this.scopes.Count;

            var firstResult = first(this);
            var secondResult = this.EnsureScopeCount(originalCount, () => second(this));

            return new StatementSyntax(firstResult, secondResult, this.scopes.Peek());
        }

        public ISyntax AnalyzeFunctionDefinition(IEnumerable<(TypeResolvable type, string name)> pars, Analyzable body) {
            var fixedPars = pars.Select(x => {
                var typeResolver = new TypeResolver(this.scopes.Peek(), x.type);
                var type = typeResolver.ResolveType();

                var location = new VariableLocation(x.name, type, true);
                return new { Name = x.name, Location = location };
            })
            .ToList();

            AnalyticScope scope = this.scopes.Peek();
            foreach (var par in fixedPars) {
                scope = scope.AddVariable(par.Name, par.Location);
            }

            int scopeCount = this.scopes.Count;
            this.scopes.Push(scope);

            var bodyResult = this.EnsureScopeCount(scopeCount, () => body(this));
            return new FunctionDefinitionSyntax(bodyResult, fixedPars.Select(x => x.Location), this.scopes.Peek());
        }

        public ISyntax AnalyzeIntrinsic(IntrinsicDescriptor kind, IEnumerable<Analyzable> args) {
            return new IntrinsicSyntax(kind, args.Select(x => x(this)), this.scopes.Peek());
        }

        public ISyntax AnalyzeBinaryExpression(Analyzable left, Analyzable right, BinaryOperator op) {
            var leftResult = left(this);
            var rightResult = right(this);

            string findFunction;
            if (op == BinaryOperator.Add) {
                findFunction = "Add";
            }
            else if (op == BinaryOperator.Subtract) {
                findFunction = "Subtract";
            }
            else if (op == BinaryOperator.Multiply) {
                findFunction = "Multiply";
            }
            else if (op == BinaryOperator.Divide) {
                findFunction = "Divide";
            }
            else {
                throw new Exception($"Unknown binary operator '{op}'");
            }

            if (this.scopes.Peek().TryGetTypeMembers(leftResult.TypeSymbol, findFunction, out var list)) {
                var func = list
                    .Where(x => x.Type is FunctionTypeSymbol)
                    .Select(x => new { Location = x, Type = (FunctionTypeSymbol)x.Type })
                    .Where(x => x.Type.ParameterTypes.Count == 2)
                    .Where(x => x.Type.ParameterTypes[0].Equals(rightResult.TypeSymbol))
                    .Where(x => x.Type.ParameterTypes[1].Equals(rightResult.TypeSymbol))
                    .ToList();

                if (func != null && func.Count == 1) {
                    return new FunctionInvokeSyntax(
                        new VariableReferenceSyntax(func.First().Location, this.scopes.Peek()),
                        new[] { leftResult, rightResult },
                        this.scopes.Peek()
                    );
                }
            }

            throw new Exception($"Type does not have a method named '{findFunction}'");
        }

        public ISyntax AnalyzeInvoke(Analyzable funcExpr, IEnumerable<Analyzable> args) {
            return new FunctionInvokeSyntax(funcExpr(this), args.Select(x => x(this)), this.scopes.Peek());
        }

        private T EnsureScopeCount<T>(int count, Func<T> action) {
            T result = action();

            while (this.scopes.Count > count) {
                this.scopes.Pop();
            }

            return result;
        }
    }
}