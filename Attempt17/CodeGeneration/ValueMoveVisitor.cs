using Attempt17.TypeChecking;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.CodeGeneration {
    public class ValueMoveVisitor : ITypeVisitor<CBlock> {
        private readonly string value;
        private readonly ICodeGenerator gen;
        private static int moveTemp = 0;
        private IScope scope;

        public ValueMoveVisitor(string value, IScope scope, ICodeGenerator gen) {
            this.value = value;
            this.scope = scope;
            this.gen = gen;
        }

        public CBlock VisitArrayType(ArrayType type) {
            var varName = "$move_temp_" + moveTemp++;
            var varType = this.gen.Generate(type);
            var writer = new CWriter();

            writer.Line("// Array move");
            writer.Line($"{varType} {varName} = {this.value};");
            writer.Line($"{this.value}.size = 0LL;");
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
            if (!this.scope.FindTypeInfo(type.Path).TryGetValue(out var info)) {
                throw new Exception("This is not supposed to happen");
            }

            return info.Accept(new IdentifierTargetVisitor<CBlock>() {
                HandleComposite = compositeInfo => {
                    if (compositeInfo.Kind == CompositeKind.Class) {
                        var varName = "$move_temp_" + moveTemp++;
                        var varType = this.gen.Generate(type);
                        var writer = new CWriter();

                        writer.Line("// Class move");
                        writer.Line($"{varType} {varName} = {this.value};");
                        writer.Line($"{this.value} = 0;");
                        writer.EmptyLine();

                        return writer.ToBlock(varName);
                    }
                    else {
                        throw new InvalidOperationException();
                    }
                }
            });
        }

        public CBlock VisitVariableType(VariableType type) {
            var varName = "$move_temp_" + moveTemp++;
            var varType = this.gen.Generate(type);
            var writer = new CWriter();

            writer.Line("// Variable move");
            writer.Line($"{varType} {varName} = {this.value};");
            writer.Line($"{this.value} = 0;");
            writer.EmptyLine();

            return writer.ToBlock(varName);
        }

        public CBlock VisitVoidType(VoidType type) {
            throw new InvalidOperationException();
        }
    }
}