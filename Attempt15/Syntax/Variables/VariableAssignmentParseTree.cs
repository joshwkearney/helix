using System;
using System.Collections.Generic;
using System.Text;
using JoshuaKearney.Attempt15.Compiling;

namespace JoshuaKearney.Attempt15.Syntax.Variables {
    public class VariableAssignmentParseTree : IParseTree {
        public string VariableName { get; }

        public IParseTree Assignment { get; }

        public IParseTree Appendix { get; }

        public VariableAssignmentParseTree(string name, IParseTree assignment, IParseTree appendix) {
            this.VariableName = name;
            this.Assignment = assignment;
            this.Appendix = appendix;
        }

        public ISyntaxTree Analyze(AnalyzeEventArgs args) {
            if (!args.Context.Variables.TryGetValue(this.VariableName, out var info)) {
                throw new Exception($"Cannot assign value to nonexistant variable '{info.Name}'");
            }

            if (info.IsImmutable) {
                throw new Exception($"Cannot assign value to immutable variable '{info.Name}'");
            }

            var assign = this.Assignment.Analyze(args);
            var appendix = this.Appendix.Analyze(args);

            if (!args.Unifier.TryUnifySyntax(assign, info.Type, out assign)) {
                throw new Exception($"Cannot assign value to variable '{info.Name}': Types don't match");
            }

            return new VariableAssignmentSyntaxTree(info, assign, appendix);
        }
    }
}
