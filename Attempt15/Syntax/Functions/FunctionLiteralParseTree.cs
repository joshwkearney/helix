using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Parsing;
using JoshuaKearney.Attempt15.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JoshuaKearney.Attempt15.Syntax.Functions {
    public class ParseFunctionParameter {
        public TypePotential Type { get; }

        public string Name { get; }

        public ParseFunctionParameter(string name, TypePotential type) {
            this.Name = name;
            this.Type = type;
        }
    }

    public class FunctionLiteralParseTree : IParseTree {
        public IReadOnlyList<ParseFunctionParameter> Parameters { get; }

        public IParseTree Body { get; }

        public TypePotential DeclaredType { get; }

        public FunctionLiteralParseTree(IParseTree body, IEnumerable<ParseFunctionParameter> pars, TypePotential declaredType) {
            this.Body = body;
            this.Parameters = pars.ToArray();
            this.DeclaredType = declaredType;
        }

        public ISyntaxTree Analyze(AnalyzeEventArgs args) {
            var declaredType = this.DeclaredType(args.Context);

            // Analyze the parameters
            var pars = this.Parameters
                .Select(x => new IdentifierInfo(x.Name, x.Type(args.Context)))
                .ToArray();

            // Add the optional declared type into the scope
            // It will be null if there is no declared type (inferred)
            var newScope = args.Context;
            if (declaredType == null) {
                newScope = newScope.PushFunctionType(null);
            }
            else { 
                var funcType = new SimpleFunctionType(declaredType, pars.Select(x => x.Type));
                newScope = args.Context.PushFunctionType(funcType);
            }

            // Set the parameters into the scope
            foreach (var par in pars) {
                newScope = newScope.SetVariable(par.Name, new VariableInfo(par.Name, par.Type, true));
            }

            // Analyze the body and ensure the return type can unify with the declared type
            var body = this.Body.Analyze(args.SetContext(newScope));
            if (declaredType != null) {
                if (!args.Unifier.TryUnifySyntax(body, declaredType, out body)) {
                    throw new Exception();
                }
            }            

            var result = new FunctionLiteralSyntaxTree(
                body:   body,
                pars:   pars
            );

            // Make sure we're not closing on mutable variables
            foreach (var closed in result.ExpressionType.ClosedVariables) {
                if (!closed.IsImmutable) {
                    throw new Exception($"Cannot close over the mutable variable '{closed.Name}'");
                }
            }

            return result;
        }
    }
}
