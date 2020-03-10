using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.CodeGeneration {
    public class ValueMoveVisitor : ITypeVisitor<CBlock> {
        private readonly string value;
        private readonly ICodeGenerator gen;
        private static int moveTemp = 0;

        public ValueMoveVisitor(string value, ICodeGenerator gen) {
            this.value = value;
            this.gen = gen;
        }

        public CBlock VisitArrayType(ArrayType type) {
            var varName = "$move_temp_" + moveTemp++;
            var varType = this.gen.Generate(type);
            var writer = new CWriter();

            writer.Line("// Array move");
            writer.Line($"{varType} {varName} = {this.value};");
            writer.Line($"{this.value}.length = 0LL;");
            writer.Line($"{this.value}.data = 0LL;");
            writer.EmptyLine();

            return writer.ToBlock(varName);
        }

        public CBlock VisitBoolType(BoolType type) {
            throw new InvalidOperationException();
        }

        public CBlock VisitIntType(IntType type) {
            throw new InvalidOperationException();
        }

        public CBlock VisitNamedType(NamedType type) {
            throw new InvalidOperationException();
        }

        public CBlock VisitVariableType(VariableType type) {
            var varName = "$move_temp_" + moveTemp++;
            var varType = this.gen.Generate(type);
            var writer = new CWriter();

            writer.Line("// Variable move");
            writer.Line($"{varType} {varName} = {this.value};");
            writer.Line($"{this.value} = ({varType})NULL;");
            writer.EmptyLine();

            return writer.ToBlock(varName);
        }

        public CBlock VisitVoidType(VoidType type) {
            throw new InvalidOperationException();
        }
    }
}