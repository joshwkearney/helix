namespace Helix.Common {
    public sealed class Assert {
        public static void IsTrue(bool value) {
            if (!value) {
                throw new InvalidOperationException("Assertion failed!");
            }
        }

        public static void IsFalse(bool value) {
            if (value) {
                throw new InvalidOperationException("Assertion failed!");
            }
        }
    }
}
