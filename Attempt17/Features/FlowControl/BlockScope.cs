using Attempt17.TypeChecking;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.Features.FlowControl {
    public class BlockScope : ITypeCheckScope {
        private readonly ITypeCheckScope head;

        private readonly Dictionary<IdentifierPath, ImmutableHashSet<VariableCapture>> capturingVariables
            = new Dictionary<IdentifierPath, ImmutableHashSet<VariableCapture>>();

        private readonly HashSet<IdentifierPath> declaredVariables = new HashSet<IdentifierPath>();

        private readonly HashSet<IdentifierPath> movableVariables = new HashSet<IdentifierPath>();

        private readonly HashSet<IdentifierPath> movedVariables = new HashSet<IdentifierPath>();

        private readonly Dictionary<(LanguageType type, string methodName), IdentifierPath> methods
            = new Dictionary<(LanguageType type, string methodName), IdentifierPath>();

        public IdentifierPath Path { get; }

        public BlockScope(IdentifierPath path, ITypeCheckScope head) {
            this.Path = path;
            this.head = head;
        }

        public IOption<TypeInfo> FindTypeInfo(IdentifierPath path) {
            return this.head.FindTypeInfo(path);
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

        public void SetTypeInfo(IdentifierPath path, TypeInfo info) {
            this.head.SetTypeInfo(path, info);

            if (info.AsVariableInfo().TryGetValue(out var varinfo)) {
                this.declaredVariables.Add(varinfo.Path);
            }
        }

        public void SetVariableMovable(IdentifierPath path, bool isMovable) {
            if (this.declaredVariables.Contains(path)) {
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
            if (this.declaredVariables.Contains(path)) {
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

        public void SetMethod(LanguageType type, string methodName, IdentifierPath methodLocation) {
            this.methods[(type, methodName)] = methodLocation;
        }

        public IOption<FunctionInfo> FindMethod(LanguageType type, string methodName) {
            return this.methods
                .GetValueOption((type, methodName))
                .SelectMany(this.FindFunction)
                .GetValueOr(() => this.head.FindMethod(type, methodName));
        }
    }
}