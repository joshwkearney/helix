using System;
using System.Linq;
using System.Text;

namespace Attempt10 {
    public interface ITrophyTypeVisitor {
        void Visit(PrimitiveTrophyType value);
        void Visit(ClosureTrophyType value);
        void Visit(FunctionTrophyType value);
    }

    public interface ITrophyType : IEquatable<ITrophyType> {
        bool IsCompatibleWith(ITrophyType other);

        void Accept(ITrophyTypeVisitor visitor);
    }
}