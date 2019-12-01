using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt3 {
    public interface ILanguageType {

    }

    public class IntPrimitive : ILanguageType {
        public static ILanguageType Int32 { get; } = new IntPrimitive();

        private IntPrimitive() { }
    }
}