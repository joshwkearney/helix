using Attempt17.Parsing;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Features.Variables {
    public class VariableStoreParseTree : IParseTree {
        public TokenLocation Location { get; }

        public IParseTree Target { get; }

        public IParseTree Value { get; }

        public VariableStoreParseTree(TokenLocation loc, IParseTree target, IParseTree value) {
            this.Location = loc;
            this.Target = target;
            this.Value = value;
        }

        public ISyntaxTree Analyze(Scope scope) {
            var target = this.Target.Analyze(scope);
            var value = this.Value.Analyze(scope);

            // Make sure the types match
            if (!(target.ReturnType is VariableType varType)) {
                throw TypeCheckingErrors.ExpectedVariableType(this.Target.Location, target.ReturnType);
            }

            // Make sure that equates have a variable type for the value
            if (varType.InnerType != value.ReturnType) {
                throw TypeCheckingErrors.UnexpectedType(this.Value.Location, varType.InnerType, value.ReturnType);
            }

            // Variables captured by the target collectively define the lifetime of 
            // the variable that is being stored into

            // Make sure the thing being stored will outlive these captured variables           
            foreach (var targetPath in target.CapturedVariables) {
                foreach (var valuePath in value.CapturedVariables) {
                    var targetScope = targetPath.Pop();
                    var valueScope = valuePath.Pop();

                    if (valueScope.StartsWith(targetScope) && valueScope != targetScope) {
                        throw TypeCheckingErrors.StoreScopeExceeded(this.Location, targetPath, valuePath);
                    }
                }
            }

            return new VariableStoreSyntaxTree(target, value);
        }
    }
}