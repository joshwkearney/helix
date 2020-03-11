﻿using System;
using Attempt17.Types;

namespace Attempt17.TypeChecking {
    public class CircularValueObjectDetector : ITypeVisitor<bool> {
        private readonly LanguageType containingType;
        private readonly IScope scope;

        public CircularValueObjectDetector(LanguageType containingType, IScope scope) {
            this.containingType = containingType;
            this.scope = scope;
        }

        public bool VisitArrayType(ArrayType type) {
            return false;
        }

        public bool VisitBoolType(BoolType type) {
            return false;
        }

        public bool VisitIntType(IntType type) {
            return false;
        }

        public bool VisitNamedType(NamedType type) {
            if (!this.scope.FindTypeInfo(type.Path).TryGetValue(out var info)) {
                throw new Exception("This isn't supposed to happen");
            }

            return info.Match(
                varInfo => throw new InvalidOperationException(),
                funcInfo => this.containingType == type,
                structInfo => {
                    foreach (var mem in structInfo.Signature.Members) {
                        if (mem.Type == this.containingType) {
                            return true;
                        }

                        if (mem.Type.Accept(this)) {
                            return true;
                        }
                    }

                    return false;
                });
        }

        public bool VisitVariableType(VariableType type) {
            return false;
        }

        public bool VisitVoidType(VoidType type) {
            return false;
        }
    }
}