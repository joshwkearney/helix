using System;

namespace JoshuaKearney.Attempt15.Compiling {
    public class CodeGenerateEventArgs : EventArgs {
        public ICodeGenerator CodeGenerator { get; }

        public MemoryManager MemoryManager { get; }

        public FunctionCodeGenerator FunctionGenerator { get; }

        public TupleCodeGenerator TupleGenerator { get; }

        public bool DoesValueEscape { get; }

        public CodeGenerateEventArgs(ICodeGenerator codeGen, FunctionCodeGenerator funcGen, TupleCodeGenerator tupleGen, MemoryManager mem, bool doesEscape = false) {
            this.CodeGenerator = codeGen;
            this.MemoryManager = mem;
            this.FunctionGenerator = funcGen;
            this.TupleGenerator = tupleGen;
            this.DoesValueEscape = doesEscape;
        }

        public CodeGenerateEventArgs WithEscape(bool doesValueEscape) {
            return new CodeGenerateEventArgs(
                this.CodeGenerator, 
                this.FunctionGenerator, 
                this.TupleGenerator, 
                this.MemoryManager,
                doesValueEscape
            );
        }
    }
}