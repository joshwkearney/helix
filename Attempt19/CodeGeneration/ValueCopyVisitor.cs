using Attempt19.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt19.CodeGeneration {
    public class ValueCopyVisitor : ITypeVisitor<CBlock> {
        private readonly string value;
        private readonly ICodeGenerator gen;
        private readonly ICScope scope;
        private static int arrayCopyTemp = 0;
        private static int structCopyTemp = 0;
        private static int varCopyTemp = 0;

        public ValueCopyVisitor(string value, ICodeGenerator gen, ICScope scope) {
            this.value = value;
            this.gen = gen;
            this.scope = scope;
        }

        public CBlock VisitArrayType(ArrayType type) {
            var tempName = "$array_copy_" + arrayCopyTemp++;
            var tempType = this.gen.Generate(type);
            var writer = new CWriter();

            writer.Line("// Array copy");
            writer.VariableInit(tempType, tempName, this.value);
            writer.Line($"{tempName}.data &= ~1;");
            writer.EmptyLine();

            return writer.ToBlock(tempName);
        }

        public CBlock VisitBoolType(BoolType type) {
            return new CBlock(this.value);
        }

        public CBlock VisitIntType(IntType type) {
            return new CBlock(this.value);
        }

        public CBlock VisitVariableType(VariableType type) {
            var tempName = "$var_copy_" + varCopyTemp++;
            var tempType = this.gen.Generate(type);
            var writer = new CWriter();

            writer.Line("// Var copy");
            writer.VariableInit(tempType, tempName, this.value + " & ~1");
            writer.EmptyLine();

            return writer.ToBlock(tempName);
        }

        public CBlock VisitVoidType(VoidType type) {
            return new CBlock(this.value);
        }
    }
}