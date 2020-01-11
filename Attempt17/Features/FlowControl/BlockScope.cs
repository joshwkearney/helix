using Attempt17.TypeChecking;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.Features.FlowControl {
    public class BlockScope : IScope {
        private readonly IScope head;

        private readonly Dictionary<IdentifierPath, VariableInfo> variables 
            = new Dictionary<IdentifierPath, VariableInfo>();

        private readonly Dictionary<IdentifierPath, FunctionInfo> functions
            = new Dictionary<IdentifierPath, FunctionInfo>();

        private readonly Dictionary<IdentifierPath, ImmutableHashSet<VariableCapture>> capturingVariables
            = new Dictionary<IdentifierPath, ImmutableHashSet<VariableCapture>>();

        private readonly HashSet<IdentifierPath> movableVariables = new HashSet<IdentifierPath>();

        private readonly HashSet<IdentifierPath> movedVariables = new HashSet<IdentifierPath>();

        public IdentifierPath Path { get; }

        public BlockScope(IdentifierPath path, IScope head) {
            this.Path = path;
            this.head = head;
        }

        public IOption<FunctionInfo> FindFunction(IdentifierPath path) {
            return this.functions
                .GetValueOption(path)
                .GetValueOr(() => this.head.FindFunction(path));
        }

        public IOption<VariableInfo> FindVariable(IdentifierPath path) {
            return this.variables
                .GetValueOption(path)
                .GetValueOr(() => this.head.FindVariable(path));
        }

        public ImmutableHashSet<VariableCapture> GetCapturingVariables(IdentifierPath path) {
            return this.capturingVariables
                .GetValueOption(path)
                .GetValueOr(() => ImmutableHashSet<VariableCapture>.Empty)
                .Union(this.head.GetCapturingVariables(path));
        }

        public bool IsVariableMovable(IdentifierPath path) {
            return this.movableVariables.Contains(path)
                || this.head.IsVariableMovable(path);
        }

        public bool IsVariableMoved(IdentifierPath path) {
            return this.movedVariables.Contains(path)
                || this.head.IsVariableMoved(path);
        }

        public void SetCapturingVariable(VariableCapture capturing, IdentifierPath captured) {
            if (!this.capturingVariables.ContainsKey(captured)) {
                this.capturingVariables[captured] = ImmutableHashSet<VariableCapture>.Empty;
            }

            this.capturingVariables[captured] = this.capturingVariables[captured].Add(capturing);
        }

        public void SetFunction(IdentifierPath path, FunctionInfo info) {
            this.functions[path] = info;
        }

        public void SetVariable(IdentifierPath path, VariableInfo info) {
            this.variables[path] = info;
        }

        public void SetVariableMovable(IdentifierPath path, bool isMovable) {
            if (this.variables.ContainsKey(path)) {
                if (isMovable) {
                    this.movableVariables.Add(path);
                }
                else {
                    this.movableVariables.Remove(path);
                }
            }
            else {
                this.head.SetVariableMovable(path, isMovable);
            }
        }

        public void SetVariableMoved(IdentifierPath path, bool isMoved) {
            if (this.variables.ContainsKey(path)) {
                if (isMoved) {
                    this.movedVariables.Add(path);
                }
                else {
                    this.movedVariables.Remove(path);
                }
            }
            else {
                this.head.SetVariableMoved(path, isMoved);
            }
        }
    }
}