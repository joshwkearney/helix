using System;
namespace Attempt17.CodeGeneration {
    public class CodeGenerationContext {      
        public ICScope Scope { get; }

        public ICodeGenerator Generator { get; }

        public CodeGenerationContext(ICScope scope, ICodeGenerator gen) {

            Scope = scope;
            this.Generator = gen;
        }

        public CodeGenerationContext WithScope(ICScope scope) {
            return new CodeGenerationContext(scope, this.Generator);
        }
    }
}