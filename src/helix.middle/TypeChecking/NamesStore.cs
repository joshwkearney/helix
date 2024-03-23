namespace Helix.MiddleEnd.TypeChecking {
    internal class NamesStore {
        private int counter = 0;

        public string GetConvertName() {
            return "$convert_" + this.counter++;
        }

        public string GetLoopUnrollPrefix() {
            return "$unroll" + this.counter++;
        }
    }
}
