using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Trophy.Analysis;
using Trophy.Parsing;

namespace Trophy.Features.FlowControl {
    public class MatchSyntaxA : ISyntaxA {
        public TokenLocation Location { get; }

        public ISyntaxA Argument { get; }

        public IReadOnlyList<string> Patterns { get; }

        public IReadOnlyList<ISyntaxA> PatternExpressions { get; }

        public ISyntaxB CheckNames(INameRecorder names) {
            throw new NotImplementedException();
        }
    }
}