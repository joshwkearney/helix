//using Attempt20.Analysis;
//using Attempt20.Parsing;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Attempt20.src.Features.FlowControl {
//    public class MatchParsedSyntax : IParsedSyntax {
//        public TokenLocation Location { get; set; }

//        public IParsedSyntax Target { get; set; }

//        public IReadOnlyList<Pattern> Patterns { get; set; }

//        public IReadOnlyList<IParsedSyntax> PatternExpressions { get; set; }

//        public IParsedSyntax CheckNames(INameRecorder names) {
//            this.Target = this.Target.CheckNames(names);
//            this.PatternExpressions = this.PatternExpressions.Select(x => x.CheckNames(names)).ToArray();

//            return this;
//        }

//        public ISyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
//            var target = this.Target.CheckTypes(names, types);

//            // If the default case isn't the last case, there is a problem
//            if (this.Patterns.SkipLast(1).Any(x => x.IsDefaultPattern())) {
//                // Exception
//            }

//            if (target.ReturnType.IsIntType) {
//                // Make sure the last pattern is a default pattern
//                if (!this.Patterns.Last().IsDefaultPattern()) {
//                    // Exception
//                }

//                // Make sure all the patterns are int patterns
//                if (!this.Patterns.SkipLast(1).All(x => x.AsIntPattern().Any())) {
//                    // Exception
//                }

//                // Return
//            }
//            else if (target.ReturnType.AsNamedType().TryGetValue(out var path) && types.TryGetUnion(path).TryGetValue(out var unionSig)) {
//                // If the target is a union, make sure all the patterns are either default patterns or identifier patterns

//            }
//        }
//    }
//}