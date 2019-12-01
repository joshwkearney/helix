using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt12 {
    public delegate ISyntaxTree SyntaxPotential(Scope scope);

    public class Analyzer {
        public SyntaxPotential AnalyzeMemberAccess(SyntaxPotential targetPotential, string name) {
            return scope => {
                var target = targetPotential(scope);

                if (scope.TypeScopes.TryGetValue(target.ExpressionType, out var typescope)) {
                    if (typescope.FunctionMembers.TryGetValue(name, out var func)) {
                        return func;
                    }
                }

                throw new Exception();
            };
        }

        public SyntaxPotential AnalyzeInvoke(SyntaxPotential targetPotential, IReadOnlyList<SyntaxPotential> argsPotential) {
            return (scope) => {
                var target = targetPotential(scope);                

                if (target.ExpressionType is TrophyFunctionType funcType) {
                    var args = argsPotential.Select(x => x(scope)).ToArray();

                    if (funcType.ParameterTypes.Count != args.Length) {
                        throw new Exception();
                    }

                    foreach (var (t1, t2) in args.Zip(funcType.ParameterTypes, (x, y) => (x.ExpressionType, y))) {
                        if (!t1.Equals(t2)) {
                            throw new Exception();
                        }
                    }

                    return new FunctionInvokeSyntax(target, funcType.ReturnType, scope, args);
                }
                else {
                    throw new Exception();
                }
            };
        }

        public SyntaxPotential AnalyzeFunctionLiteral(IReadOnlyList<string> argNames, IReadOnlyList<ITrophyType> argTypes, SyntaxPotential bodyFactory, ITrophyType returnType = null) {
            return (scope) => {
                if (argNames.Count != argTypes.Count) {
                    throw new Exception();
                }

                foreach (var name in argNames) {
                    if (scope.Variables.ContainsKey(name)) {
                        throw new Exception("Function arguments cannot shadow local variables");
                    }
                }

                var args = new List<FunctionParameter>();
                var newScope = scope;

                if (returnType == null) {
                    newScope = newScope.SetEnclosedFunctionType(null);
                }
                else {
                    newScope = newScope.SetEnclosedFunctionType(new TrophyFunctionType(returnType, argTypes));
                }

                // Add arguments to local scope
                foreach (var (name, type) in argNames.Zip(argTypes, (x, y) => (name: x, type: y))) {
                    newScope = newScope.SetVariable(name, type, scope.ClosureLevel + 1);
                    args.Add(new FunctionParameter(name, type));
                }

                newScope = newScope.IncrementClosureLevel();

                // Analyze body with full abstractions
                var body = bodyFactory(newScope);
                var closed = new ClosureAnalyzer(body).GetClosedVariables();

                var funcType = new TrophyFunctionType(returnType ?? body.ExpressionType, argTypes);
                var syntax = new FunctionLiteralSyntax(funcType, body, scope, closed, args);

                return syntax;
            };
        }

        public SyntaxPotential AnalyzeEvoke() {
            return scope => {
                if (scope.EnclosedFunctionType == null) {
                    throw new Exception("Recursive functions must have explicitly defined return types");
                }

                return new FunctionRecurseLiteral(scope.EnclosedFunctionType, scope);
            };
        }

        public SyntaxPotential AnalyzeIfExpression(SyntaxPotential conditionPotential, SyntaxPotential affirmativePotential, SyntaxPotential negativePotential) {
            return (scope) => {
                var affirmative = affirmativePotential(scope);
                var negative = negativePotential(scope);
                var condition = conditionPotential(scope);

                if (affirmative.ExpressionType != negative.ExpressionType) {
                    throw new Exception("Branches of an if expression must have the same return type");
                }

                if (condition.ExpressionType != PrimitiveTrophyType.Boolean) {
                    throw new Exception("The condition of an if statement must be a boolean");
                }

                return new IfExpressionSyntax(condition, affirmative, negative, affirmative.ExpressionType, scope);
            };
        }

        public SyntaxPotential AnalyzeConstantDefinition(string name, SyntaxPotential assignPotential, SyntaxPotential appendixFactory) {
            return (scope) => {
                if (scope.Variables.ContainsKey(name)) {
                    throw new Exception($"Variable '{name}' is already defined in the current scope");
                }

                var assign = assignPotential(scope);

                var newScope = scope.SetVariable(name, assign.ExpressionType, scope.ClosureLevel);
                var appendix = appendixFactory(newScope);

                return new VariableDefinitionSyntax(name, assign, appendix, scope);
            };
        }

        public SyntaxPotential AnalyzeBinaryOperator(SyntaxPotential left, SyntaxPotential right, string op) {
            return scope => {
                var leftSyntax = left(scope);
                var rightSyntax = right(scope);

                if (!scope.TypeScopes.TryGetValue(leftSyntax.ExpressionType, out var typeScope)) {
                    throw new Exception($"Could not find function '{op}' for the current types and scope");
                }

                if (!typeScope.FunctionMembers.TryGetValue(op, out var func)) {
                    throw new Exception($"Could not find function '{op}' for the current types and scope");
                }

                if (func.Parameters.Count != 2 || !func.Parameters[0].Type.Equals(rightSyntax.ExpressionType)) {
                    throw new Exception($"Function '{op}' has the incorrect number or type of parameters");
                }

                return new FunctionInvokeSyntax(
                    func,
                    func.ReturnType,
                    scope, 
                    new[] { leftSyntax, rightSyntax });                   

                throw new Exception();
            };
        }      

        public SyntaxPotential AnalyzeUnaryOperator(SyntaxPotential operand, string op) {
            return scope => {
                var syntax = operand(scope);

                if (!scope.TypeScopes.TryGetValue(syntax.ExpressionType, out var typeScope)) {
                    throw new Exception($"Could not find function '{op}' for the current types and scope");
                }

                if (!typeScope.FunctionMembers.TryGetValue(op, out var func)) {
                    throw new Exception($"Could not find function '{op}' for the current types and scope");
                }

                if (func.Parameters.Count != 1) {
                    throw new Exception($"Function '{op}' has the incorrect number or type of parameters");
                }

                return new FunctionInvokeSyntax(
                    func,
                    func.ReturnType,
                    scope,
                    new[] { syntax });

                throw new Exception();
            };
        }

        public SyntaxPotential AnalyzeVariableLiteral(string name) {
            return (scope) => {
                if (!scope.Variables.TryGetValue(name, out var info)) {
                    throw new Exception($"Variable '{name}' does not exist in the current scope");
                }

                return new VariableLiteralSyntax(name, info.Type, scope);
            };            
        }

        public SyntaxPotential AnalyzeInt64Literal(long value) {
            return (scope) => new Int64LiteralSyntax(value, scope);
        }

        public SyntaxPotential AnalyzeBoolLiteral(bool value) {
            return (scope) => new BoolLiteralSyntax(value, scope);
        }

        public SyntaxPotential AnalyzeReal64Literal(double value) {
            return (scope) => new Real64Literal(value, scope);
        }
    }
}