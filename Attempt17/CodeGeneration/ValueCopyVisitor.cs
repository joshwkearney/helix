using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.CodeGeneration {
    public class ValueCopyVisitor : ITypeVisitor<CBlock> {
        private readonly string value;
        private readonly ICodeGenerator gen;
        private readonly ICScope scope;
        private int arrayCopyTemp = 0;

        public ValueCopyVisitor(string value, ICodeGenerator gen, ICScope scope) {
            this.value = value;
            this.gen = gen;
            this.scope = scope;
        }

        public CBlock VisitArrayType(ArrayType type) {
            var tempName = "$array_copy_" + this.arrayCopyTemp++;
            var tempType = this.gen.Generate(type);
            var writer = new CWriter();

            writer.Line("// Array copy");
            writer.VariableInit(tempType, tempName, this.value);
            writer.Line($"{tempName}.data &= ~1;");
            writer.EmptyLine();

            this.scope.SetVariableUndestructed(tempName, type);

            return writer.ToBlock(tempName);
        }

        public CBlock VisitBoolType(BoolType type) {
            return new CBlock(this.value);
        }

        public CBlock VisitIntType(IntType type) {
            return new CBlock(this.value);
        }

        public CBlock VisitNamedType(NamedType type) {
            // TODO - Fix this

            return new CBlock(this.value);
        }

        public CBlock VisitVariableType(VariableType type) {
            var varTypeName = this.gen.Generate(type);

            return new CBlock(CWriter.MaskPointer(this.value));
        }

        public CBlock VisitVoidType(VoidType type) {
            return new CBlock(this.value);
        }
    }
}