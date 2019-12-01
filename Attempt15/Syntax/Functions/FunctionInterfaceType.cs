using JoshuaKearney.Attempt15.Compiling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JoshuaKearney.Attempt15.Types {
    public class FunctionInterfaceType : ITrophyType, IFunctionType, IEquatable<FunctionInterfaceType> {
        public TrophyTypeKind Kind => TrophyTypeKind.FunctionInterface;

        public ITrophyType ReturnType { get; }

        public IReadOnlyList<ITrophyType> ArgTypes { get; }

        public bool IsReferenceCounted => true;

        public FunctionInterfaceType(ITrophyType returnType, IEnumerable<ITrophyType> argTypes) {
            this.ReturnType = returnType;
            this.ArgTypes = argTypes.ToArray();
        }

        public bool Equals(FunctionInterfaceType other) {
            if (!this.ReturnType.Equals(other.ReturnType)) {
                return false;
            }

            if (!this.ArgTypes.SequenceEqual(other.ArgTypes)) {
                return false;
            }

            return true;
        }

        public override bool Equals(object obj) {
            if (obj is FunctionInterfaceType func) {
                return this.Equals(func);
            }

            return false;
        }

        public override int GetHashCode() {
            return this.ArgTypes
                .Prepend(this.ReturnType)
                .Aggregate(37, (x, y) => x + 7 * y.GetHashCode());
        }

        public string GenerateName(CodeGenerateEventArgs args) {
            return args.FunctionGenerator.GenerateFunctionInterfaceTypeName(this, args);
        }
    }
}