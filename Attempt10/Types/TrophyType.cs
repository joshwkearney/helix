using System;
using System.Linq;
using System.Text;

namespace Attempt12 {
    public interface ITrophyTypeVisitor {
        void Visit(PrimitiveTrophyType value);
        void Visit(TrophyFunctionType value);
    }

    public interface ITrophyType : IEquatable<ITrophyType> {
        bool IsLiteral { get; }
        bool IsMovable { get; }

        bool IsCompatibleWith(ITrophyType other);
        void Accept(ITrophyTypeVisitor visitor);
    }
}