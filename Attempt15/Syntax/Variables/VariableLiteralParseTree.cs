using JoshuaKearney.Attempt15.Compiling;
using System;

namespace JoshuaKearney.Attempt15.Syntax.Variables {
    public class VariableLiteralParseTree : IParseTree {
        public string VariableName { get; }

        public VariableLiteralParseTree(string name) {
            this.VariableName = name;
        }

        public ISyntaxTree Analyze(AnalyzeEventArgs args) {
            if (!args.Context.Variables.TryGetValue(this.VariableName, out var info)) {
                throw new Exception();
            }

            return new VariableLiteralSyntaxTree(
                name:           this.VariableName,
                type:           info.Type,
                isImmutable:    info.IsImmutable
            );
        }
    }
}
