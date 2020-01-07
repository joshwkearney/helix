using Attempt17.Features.Functions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt17 {
    public class Scope {
        private readonly ImmutableDictionary<IdentifierPath, object> identifierTargets;

        public IdentifierPath Path { get; }

        public int NextBlockId { get; }

        public Scope() {
            this.identifierTargets = ImmutableDictionary<IdentifierPath, object>.Empty;
            this.Path = new IdentifierPath();
            this.NextBlockId = 0;
        }

        private Scope(IdentifierPath path, int nextBlockId, ImmutableDictionary<IdentifierPath, object> variables) {
            this.Path = path;
            this.NextBlockId = nextBlockId;
            this.identifierTargets = variables;
        }

        private IOption<object> FindIdentifierTarget(string name) {
            var path = this.Path.Append("blank");

            while (!path.Segments.IsEmpty) {
                path = path.Pop();

                if (this.identifierTargets.TryGetValue(path.Append(name), out var info)) {
                    return Option.Some(info);
                }
            }

            return Option.None<object>();
        }

        public bool NameExists(string name) {
            return this.FindIdentifierTarget(name).Any();
        }

        public IOption<VariableInfo> FindVariable(string name) {
            return this.FindIdentifierTarget(name)
                .Where(x => x is VariableInfo)
                .Select(x => (VariableInfo)x);
        }

        public IOption<VariableInfo> FindVariable(IdentifierPath path) {
            if (this.identifierTargets.TryGetValue(path, out object value)) {
                if (value is VariableInfo info) {
                    return Option.Some(info);
                }
            }

            return Option.None<VariableInfo>();
        }

        public IOption<FunctionInfo> FindFunction(string name) {
            return this.FindIdentifierTarget(name)
                .Where(x => x is FunctionInfo)
                .Select(x => (FunctionInfo)x);
        }

        public IOption<FunctionInfo> FindFunction(IdentifierPath path) {
            if (this.identifierTargets.TryGetValue(path, out object value)) {
                if (value is FunctionInfo sig) {
                    return Option.Some(sig);
                }
            }

            return Option.None<FunctionInfo>();
        }

        public Scope SelectPath(Func<IdentifierPath, IdentifierPath> func) {
            return new Scope(
                func(this.Path), 
                this.NextBlockId, 
                this.identifierTargets);
        }

        public Scope AppendVariable(string name, VariableInfo info) {
            return new Scope(
                this.Path,
                this.NextBlockId,
                this.identifierTargets.Add(this.Path.Append(name), info));
        }

        public Scope AppendFunction(FunctionSignature sig) {
            var path = this.Path.Append(sig.Name);
            var info = new FunctionInfo(path, sig);

            return new Scope(
                this.Path,
                this.NextBlockId,
                this.identifierTargets.Add(path, info));
        }

        public Scope WithNextBlockId(int id) {
            return new Scope(
                this.Path,
                id,
                this.identifierTargets);
        }
    }
}