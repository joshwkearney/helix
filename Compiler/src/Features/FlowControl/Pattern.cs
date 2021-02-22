//namespace Attempt20.src.Features.FlowControl {
//    public abstract class Pattern {
//        public static Pattern IntPattern(int value) {
//            return new IntPatternImpl(value);
//        }

//        public static Pattern IdentifierPattern(string value) {
//            return new IdentifierPatternImpl(value);
//        }

//        public static Pattern DefaultPattern() {
//            return new DefaultPatternImpl();
//        }

//        private Pattern() { }

//        public virtual IOption<int> AsIntPattern() {
//            return Option.None<int>();
//        }

//        public virtual IOption<string> AsIdentifierPattern() {
//            return Option.None<string>();
//        }

//        public virtual bool IsDefaultPattern() { return false; }

//        private class DefaultPatternImpl : Pattern {
//            public DefaultPatternImpl() { }

//            public override bool IsDefaultPattern() {
//                return true;
//            }
//        }

//        private class IntPatternImpl : Pattern {
//            private readonly int value;

//            public IntPatternImpl(int value) {
//                this.value = value;
//            }

//            public override IOption<int> AsIntPattern() {
//                return Option.Some(this.value);
//            }
//        }

//        private class IdentifierPatternImpl : Pattern {
//            private readonly string value;
//            public IdentifierPatternImpl(string value) {
//                this.value = value;
//            }

//            public override IOption<string> AsIdentifierPattern() {
//                return Option.Some(this.value);
//            }
//        }
//    }
//}
