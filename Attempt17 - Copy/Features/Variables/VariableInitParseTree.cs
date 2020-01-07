using Attempt17.Parsing;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Features.Variables {
    public class VariableInitParseTree : IParseTree {
        public TokenLocation Location { get; }

        public string Name { get; }

        public IParseTree Value { get; }

        public VariableInitKind Kind { get; }

        public VariableInitParseTree(VariableInitKind kind, TokenLocation loc, string name, IParseTree value) {
            this.Location = loc;
            this.Name = name;
            this.Kind = kind;
            this.Value = value;
        }

        public ISyntaxTree Analyze(Scope scope) {
            if (scope.NameExists(this.Name)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.Name);
            }

            var value = this.Value.Analyze(scope);

            if (this.Kind == VariableInitKind.Equate) {
                if (!(value.ReturnType is VariableType)) {
                    throw TypeCheckingErrors.ExpectedVariableType(this.Value.Location, value.ReturnType);
                }
            }
               
            return new VariableInitSyntaxTree(this.Kind, this.Name, value);
        }
    }
}