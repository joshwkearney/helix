using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt2 {
    public class CompileResult<T> {
        public Exception Error { get; }

        public bool HasResult { get; }

        public T Result { get; }

        public CompileResult(Exception ex) {
            this.HasResult = false;
            this.Result = default;
            this.Error = ex;
        }

        public CompileResult(T result) {
            this.HasResult = true;
            this.Error = default;
            this.Result = result;
        }

        public static implicit operator CompileResult<T>(T result) {
            return new CompileResult<T>(result);
        }

        public static implicit operator CompileResult<T>(Exception ex) {
            return new CompileResult<T>(ex);
        }
    }
}