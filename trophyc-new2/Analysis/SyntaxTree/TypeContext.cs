using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trophy.Analysis.SyntaxTree
{
    public class TypeContext
    {
        private readonly bool isLhs = false;

        public static TypeContext None { get; } = new TypeContext(false);

        public static TypeContext LeftHandSide { get; } = new TypeContext(true);

        private TypeContext(bool isLhs)
        {
            this.isLhs = isLhs;
        }

        public override bool Equals(object? obj)
        {
            return obj is TypeContext type && isLhs == type.isLhs;
        }

        public override int GetHashCode()
        {
            return isLhs.GetHashCode();
        }

        public static bool operator ==(TypeContext? type1, TypeContext? type2)
        {
            if (type1 == null)
            {
                return type2 == null;
            }

            return type1.Equals(type2);
        }

        public static bool operator !=(TypeContext? type1, TypeContext? type2)
        {
            return !(type1 == type2);
        }
    }
}
