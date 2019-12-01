using System;
using System.Collections.Generic;
using System.Linq;
using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;

namespace JoshuaKearney.Attempt15.Syntax.Functions {
    public class FunctionCallParseTree : IParseTree {
        public IParseTree Target { get; }

        public IReadOnlyList<IParseTree> Arguments { get; }

        public FunctionCallParseTree(IParseTree target, IEnumerable<IParseTree> args) {
            this.Target = target;
            this.Arguments = args.ToArray();
        }

        public ISyntaxTree Analyze(AnalyzeEventArgs args) {
            var target = this.Target.Analyze(args);

            if (!(target.ExpressionType is IFunctionType funcType)) {
                throw new Exception();
            }

            if (funcType.ArgTypes.Count != this.Arguments.Count) {
                throw new Exception();
            }

            var analyzedArgs = this.Arguments.Select(x => x.Analyze(args)).ToArray();
            var zippedTypes = analyzedArgs.Zip(funcType.ArgTypes, (x, y) => new {
                ActualType = x.ExpressionType,
                ExpectedType = y
            })
            .ToArray();

            for (int i = 0; i < zippedTypes.Length; i++) {
                if (args.Unifier.TryUnifySyntax(analyzedArgs[i], zippedTypes[i].ExpectedType, out var result)) {
                    analyzedArgs[i] = result;
                }
                else {
                    throw new Exception();
                }
            }

            return new FunctionCallSyntaxTree(
                target:     target,
                targetType: funcType,
                args:       analyzedArgs
            );
        }
    }
}
