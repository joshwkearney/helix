using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt9 {
    public interface ITrophyTypeVisitor {
        void Visit(PrimitiveTrophyType value);
        void Visit(ReferenceType value);
        void Visit(NamedStructType value);
    }

    public interface ITrophyType {
        void Accept(ITrophyTypeVisitor visitor);
    }

    public class FunctionType : ITrophyType {


        public void Accept(ITrophyTypeVisitor visitor) {
            throw new NotImplementedException();
        }
    }

    public class NamedStructType : ITrophyType {
        public IReadOnlyDictionary<string, ITrophyType> Members { get; }

        public NamedStructType(IEnumerable<KeyValuePair<string, ITrophyType>> members) {
            this.Members = members.ToDictionary(x => x.Key, x => x.Value);
        }

        public void Accept(ITrophyTypeVisitor visitor) => visitor.Visit(this);
    }

    public class ReferenceType : ITrophyType {
        private static readonly Dictionary<ITrophyType, ITrophyType> referenceTypes = new Dictionary<ITrophyType, ITrophyType>();

        public static ITrophyType GetReferenceType(ITrophyType inner) {
            if (referenceTypes.TryGetValue(inner, out var type)) {
                return type;
            }
            else {
                var result = new ReferenceType(inner);
                referenceTypes.Add(inner, result);

                return result;
            }
        }

        public ITrophyType InnerType { get; }

        private ReferenceType(ITrophyType inner) {
            this.InnerType = inner;
        }

        public void Accept(ITrophyTypeVisitor visitor) => visitor.Visit(this);
    }

    public enum PrimitiveTrophyTypeKind {
        Int64
    }

    public class PrimitiveTrophyType : ITrophyType {
        public static ITrophyType Int64Type { get; } = new PrimitiveTrophyType(PrimitiveTrophyTypeKind.Int64);
        //public static ITrophyType Real64Type { get; } = new PrimitiveTrophyType(PrimitiveTrophyTypeKind.Real64);
        //public static ITrophyType Boolean { get; } = new PrimitiveTrophyType(PrimitiveTrophyTypeKind.Boolean);

        public PrimitiveTrophyTypeKind Kind { get; }

        private PrimitiveTrophyType(PrimitiveTrophyTypeKind kind) {
            this.Kind = kind;
        }

        public void Accept(ITrophyTypeVisitor visitor) => visitor.Visit(this);
    }
}