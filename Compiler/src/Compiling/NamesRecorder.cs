using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Attempt20.Compiling {
    public class NamesRecorder : INameRecorder {
        private readonly Stack<IdentifierPath> scopes = new Stack<IdentifierPath>();
        private readonly Stack<IdentifierPath> regions = new Stack<IdentifierPath>();
        private int nameCounter = 0;

        private readonly Dictionary<IdentifierPath, NameTarget> globalNames
                    = new Dictionary<IdentifierPath, NameTarget>();

        private readonly Stack<Dictionary<IdentifierPath, NameTarget>> localNames
            = new Stack<Dictionary<IdentifierPath, NameTarget>>();

        public IdentifierPath CurrentScope => this.scopes.Peek();

        public IdentifierPath CurrentRegion => this.regions.Peek();

        public NamesRecorder() {
            this.scopes.Push(new IdentifierPath());
            this.localNames.Push(new Dictionary<IdentifierPath, NameTarget>());
        }

        public void DeclareGlobalName(IdentifierPath path, NameTarget target) {
            this.globalNames[path] = target;
        }

        public void DeclareLocalName(IdentifierPath path, NameTarget target) {
            this.localNames.Peek()[path] = target;
        }

        public void PopScope() {
            this.scopes.Pop();
            this.localNames.Pop();
        }

        public void PushScope(IdentifierPath newScope) {
            this.scopes.Push(newScope);
            this.localNames.Push(new Dictionary<IdentifierPath, NameTarget>());
        }

        public bool TryFindName(string name, out NameTarget target, out IdentifierPath path) {
            var scope = this.CurrentScope;

            while (true) {
                path = scope.Append(name);

                if (this.TryGetName(path, out target)) {
                    return true;
                }

                if (!scope.Segments.Any()) {
                    path = new IdentifierPath(name);
                    return this.TryGetName(path, out target);
                }

                scope = scope.Pop();
            }
        }

        public bool TryGetName(IdentifierPath name, out NameTarget target) {
            foreach (var frame in this.localNames) {
                if (frame.TryGetValue(name, out target)) {
                    return true;
                }
            }

            return this.globalNames.TryGetValue(name, out target);
        }

        public void PushRegion(IdentifierPath newRegion) {
            this.regions.Push(newRegion);
        }

        public void PopRegion() {
            this.regions.Pop();
        }

        public TrophyType ResolveTypeNames(TrophyType type, TokenLocation loc) {
            if (type.IsBoolType || type.IsIntType || type.IsVoidType) {
                return type;
            }
            else if (type.AsSingularFunctionType().Any()) {
                return type;
            }
            else if (type.AsArrayType().TryGetValue(out var arrayType)) {
                return new ArrayType(this.ResolveTypeNames(arrayType.ElementType, loc));
            }
            else if (type.AsFixedArrayType().TryGetValue(out var fixedArrayType)) {
                return new FixedArrayType(this.ResolveTypeNames(fixedArrayType.ElementType, loc), fixedArrayType.Size);
            }
            else if (type.AsVariableType().TryGetValue(out var varType)) {
                return new VariableType(this.ResolveTypeNames(varType.InnerType, loc));
            }
            else if (type.AsNamedType().TryGetValue(out var name)) {
                if (!this.TryFindName(name.ToString(), out var target, out var path)) {
                    throw TypeCheckingErrors.TypeUndefined(loc, name.ToString());
                }

                if (target == NameTarget.Function) {
                    return new SingularFunctionType(path);
                }
                else if (target == NameTarget.Struct || target == NameTarget.Union) {
                    return new NamedType(path);
                }
                else {
                    throw TypeCheckingErrors.TypeUndefined(loc, name.ToString());
                }
            }
            else {
                throw new Exception();
            }
        }

        public int GetNewVariableId() {
            return this.nameCounter++;
        }
    }
}