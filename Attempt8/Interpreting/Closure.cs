using Attempt12.Analyzing;
using Attempt12.Interpreting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt12 {
    public class Closure {
        public FunctionDefinitionSyntax FunctionDefinition { get; }

        public InterprativeScope Scope { get; }

        public Closure(FunctionDefinitionSyntax func, InterprativeScope scope) {
            this.FunctionDefinition = func;
            this.Scope = scope;
        }
    }
}