namespace Helix.MiddleEnd.TypeChecking {
    internal class TypeCheckingNamesStore {
        private int counter = 0;

        public string GetConvertName() {
            return "__convert_" + this.counter++;
        }
    }
}
