using Attempt17.Features.Functions;
using Attempt17.Parsing;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Features.Variables {
    public class VariableLiteralParseTree : IParseTree {
        public TokenLocation Location { get; }

        public VariableLiteralKind Kind { get; }

        public string Name { get; }

        public VariableLiteralParseTree(TokenLocation location, string name, VariableLiteralKind kind) {
            this.Location = location;
            this.Name = name;
            this.Kind = kind;
        }

        public ISyntaxTree Analyze(Scope scope) {
            if (scope.FindVariable(this.Name).TryGetValue(out var info)) {
                if (this.Kind == VariableLiteralKind.ValueAccess) {
                    return new VariableLiteralSyntaxTree(
                        info,
                        this.Kind,
                        info.Type);
                }
                else if (this.Kind == VariableLiteralKind.LiteralAccess) {
                    return new VariableLiteralSyntaxTree(
                        info,
                        this.Kind,
                        new VariableType(info.Type));
                }
                else {
                    throw new Exception("This should never happen");
                }
            }
            else if (scope.FindFunction(this.Name).TryGetValue(out var funcInfo)) {
                return new FunctionLiteralSyntaxTree(funcInfo.Path);
            }
            else {
                throw TypeCheckingErrors.VariableUndefined(this.Location, this.Name);
            }
        }
    }
}