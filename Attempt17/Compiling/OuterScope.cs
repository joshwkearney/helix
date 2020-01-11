using Attempt17.TypeChecking;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.Compiling {
    public class OuterScope : IScope {
        private readonly Dictionary<IdentifierPath, FunctionInfo> functions
            = new Dictionary<IdentifierPath, FunctionInfo>();

        public IdentifierPath Path => new IdentifierPath();

        public IOption<FunctionInfo> FindFunction(IdentifierPath path) {
            return this.functions.GetValueOption(path);
        }

        public IOption<VariableInfo> FindVariable(IdentifierPath path) {
            return Option.None<VariableInfo>();
        }

        public ImmutableHashSet<VariableCapture> GetCapturingVariables(IdentifierPath path) {
            return ImmutableHashSet<VariableCapture>.Empty;
        }

        public bool IsVariableMovable(IdentifierPath path) => false;

        public bool IsVariableMoved(IdentifierPath path) => false;

        public void SetCapturingVariable(VariableCapture capturing, IdentifierPath captured) { }

        public void SetFunction(IdentifierPath path, FunctionInfo info) {
            this.functions[path] = info;
        }

        public void SetVariable(IdentifierPath path, VariableInfo info) { }

        public void SetVariableMovable(IdentifierPath path, bool isMovable) { }

        public void SetVariableMoved(IdentifierPath path, bool isMoved) { }
    }
}