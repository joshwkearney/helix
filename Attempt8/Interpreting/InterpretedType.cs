//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Attempt8 {
//    public interface IInterpretedValue {
//        IInterpretedValue BaseType { get; }
//    }

//    public class InterpretedRootType : IInterpretedValue {
//        public static IInterpretedValue Instance { get; } = new InterpretedRootType();

//        private InterpretedRootType() { }

//        public IInterpretedValue BaseType => this;
//    }

//    public struct Int32InterpretedValue : IInterpretedValue {
//        public int Value { get; }

//        public IInterpretedValue BaseType => InterpretedRootType.Instance;

//        public Int32InterpretedValue(int value) {
//            this.Value = value;
//        }

//        public override string ToString() {
//            return this.Value.ToString();
//        }
//    }
//}