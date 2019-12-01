using JoshuaKearney.Attempt15.Compiling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JoshuaKearney.Attempt15.Syntax.Functions {
    public class EvokeParseTree : IParseTree {
        public IReadOnlyList<IParseTree> Arguments { get; }

        public EvokeParseTree(IEnumerable<IParseTree> args) {
            this.Arguments = args.ToArray();
        }

        public ISyntaxTree Analyze(AnalyzeEventArgs args) {
            var funcType = args.Context.EnclosingFunctionReturnType.Peek();
            if (funcType == null) {
                throw new Exception();
            }

            if (funcType.ArgTypes.Count != this.Arguments.Count) {
                throw new Exception();
            }

            var analyzedArgs = this.Arguments.Select(x => x.Analyze(args)).ToArray();
            var zippedTypes = analyzedArgs.Zip(funcType.ArgTypes, (x, y) => new {
                ActualType = x.ExpressionType,
                ExpectedType = y
            });

            foreach (var pair in zippedTypes) {
                if (!pair.ExpectedType.Equals(pair.ActualType)) {
                    throw new Exception();
                }
            }

            return new EvokeSyntaxTree(
                targetType: funcType,
                args: analyzedArgs
            );
        }
    }
}
