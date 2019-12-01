using Attempt12.TypeSystem;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt12.Analyzing {
    public class FunctionDefinitionSyntax : ISyntax {
        public AnalyticScope Scope { get; }

        public ISymbol TypeSymbol { get; }

        public ISyntax FunctionBody { get; }

        public ImmutableList<VariableLocation> Parameters { get; }

        public FunctionDefinitionSyntax(ISyntax body, IEnumerable<VariableLocation> pars, AnalyticScope scope) {
            this.FunctionBody = body;
            this.Scope = scope;
            this.Parameters = pars.ToImmutableList();

            this.TypeSymbol = new FunctionTypeSymbol(
                this.FunctionBody.TypeSymbol, 
                this.Parameters.Select(x => x.Type)
            );
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}