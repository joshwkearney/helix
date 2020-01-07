using Attempt17.Parsing;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt17.Features.Functions {
    public class InvokeParseTree : IParseTree {
        public TokenLocation Location { get; }

        public IParseTree Target { get; }

        public ImmutableList<IParseTree> Arguments { get; }

        public InvokeParseTree(TokenLocation loc, IParseTree target, ImmutableList<IParseTree> args) {
            this.Location = loc;
            this.Target = target;
            this.Arguments = args;
        }

        public ISyntaxTree Analyze(Scope scope) {
            var target = this.Target.Analyze(scope);
            var args = this.Arguments.Select(x => x.Analyze(scope)).ToImmutableList();

            if (!(target.ReturnType is NamedType namedType)) {
                throw TypeCheckingErrors.ExpectedFunctionType(this.Target.Location, target.ReturnType);
            }

            if (!scope.FindFunction(namedType.Path).TryGetValue(out var info)) {
                throw TypeCheckingErrors.ExpectedFunctionType(this.Target.Location, target.ReturnType);
            }

            if (info.Signature.Parameters.Count != args.Count) {
                throw TypeCheckingErrors.ParameterCountMismatch(this.Location, info.Signature.Parameters.Count, args.Count);
            }

            var zipped = args
                .Zip(info.Signature.Parameters, (x, y) => new { 
                    ExpectedType = y.Type,
                    ActualType = x.ReturnType,
                })
                .Zip(this.Arguments, (x, y) => new { 
                    x.ActualType,
                    x.ExpectedType,
                    y.Location
                });

            foreach (var item in zipped) {
                if (item.ExpectedType != item.ActualType) {
                    throw TypeCheckingErrors.UnexpectedType(item.Location, item.ExpectedType, item.ActualType);
                }
            }

            return new InvokeSyntaxTree(info, args);
        }
    }
}