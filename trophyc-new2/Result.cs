namespace Trophy {
    public record struct Result<T> {
        private readonly T? value;
        private readonly Exception? error;

        public bool HasValue => this.error == null;

        public Result(T value) {
            this.value = value;
            this.error = null;
        }

        public Result(Exception ex) {
            this.value = default;
            this.error = ex;
        }


        public T GetValue() {
            if (this.error is Exception ex) {
                throw ex;
            }
            else {
                return this.value!;
            }
        }

        public Result<E> Select<E>(Func<T, E> selector) {
            if (this.error is Exception ex) {
                return ex;
            }
            else {
                return selector(this.value!);
            }
        }

        public Result<E> SelectMany<E>(Func<T, Result<E>> selector) {
            if (this.error is Exception ex) {
                return ex;
            }
            else {
                return selector(this.value!);
            }
        }

        public Result<T> Where(Func<T, bool> filter) {
            if (this.error is Exception ex) {
                return ex;
            }
            else if (filter(this.value!)) {
                return this;
            }
            else {
                return new InvalidOperationException(
                    $"The value of this Result<{nameof(T)}> " 
                    + "was removed by a Where() call");
            }
        }

        public IEnumerable<T> ToEnumerable() {
            if (this.HasValue) {
                yield return this.value!;
            }
        }

        public bool TryGetValue(out T value) {
            if (this.HasValue) {
                value = this.value!;
                return true;
            }
            else {
                value = default;
                return false;
            }
        }

        public static implicit operator Result<T>(T value) => new(value);

        public static implicit operator Result<T>(Exception ex) => new(ex);
    }
}