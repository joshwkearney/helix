namespace Helix.Common {
    public sealed class Assert {
        public static Exception? IsTrue(bool value) {
            if (!value) {
                throw new InvalidOperationException("Assertion failed!");
            }

            return null;
        }

        public static Exception? IsFalse(bool value) {
            if (value) {
                throw new InvalidOperationException("Assertion failed!");
            }

            return null;
        }

        public static Exception Fail() {
            throw new InvalidOperationException("Assertion failed!");
        }
    }
}
