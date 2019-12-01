using Attempt12.TypeSystem;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt12.Analyzing {
    public class FunctionInvokeSyntax : ISyntax {
        public AnalyticScope Scope { get; }

        public ISymbol TypeSymbol { get; }

        public ISyntax FunctionExpression { get; }

        public ImmutableList<ISyntax> Arguments { get; }

        public FunctionInvokeSyntax(ISyntax functionExpression, IEnumerable<ISyntax> args, AnalyticScope scope) {
            if (!(functionExpression.TypeSymbol is FunctionTypeSymbol functionType)) {
                throw new Exception("Cannot invoke non-function type");
            }

            this.FunctionExpression = functionExpression;
            this.TypeSymbol = functionType.ReturnType;
            this.Arguments = args.ToImmutableList();
            this.Scope = scope;

            if (functionType.ParameterTypes.Count != args.Count()) {
                throw new Exception("Function argument count does not match parameter count");
            }

            if (!functionType.ParameterTypes.SequenceEqual(args.Select(x => x.TypeSymbol))) {
                throw new Exception("Function argument types do not match parameter types");
            }
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}