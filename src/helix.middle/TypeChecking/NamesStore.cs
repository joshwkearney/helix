﻿namespace Helix.MiddleEnd.TypeChecking {
    internal class NamesStore {
        private int counter = 0;

        public string GetConvertName() {
            return "__convert_" + this.counter++;
        }

        public string GetLoopUnrollPrefix() {
            return "_unroll" + this.counter++;
        }
    }
}