using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;

namespace Trophy.Compiling {
    public class CWriter : ICWriter {
        private int typeCounter = 0;
        private readonly Dictionary<ITrophyType, CType> typeNames = new Dictionary<ITrophyType, CType>();

        private readonly StringBuilder decl1Sb = new StringBuilder();
        private readonly StringBuilder decl2Sb = new StringBuilder();
        private readonly StringBuilder decl3Sb = new StringBuilder();

        public CWriter() {
            this.decl1Sb.AppendLine("#include \"trophy.h\"");
            this.decl1Sb.AppendLine();
        }

        public override string ToString() {
            return new StringBuilder()
                .Append(this.decl1Sb)
                .Append(this.decl2Sb)
                .Append(this.decl3Sb)
                .ToString();
        }

        public void WriteDeclaration1(CDeclaration decl) {
            decl.WriteToC(0, this.decl1Sb);
        }

        public void WriteDeclaration2(CDeclaration decl) {
            decl.WriteToC(0, this.decl2Sb);
        }

        public void WriteDeclaration3(CDeclaration decl) {
            decl.WriteToC(0, this.decl3Sb);
        }

        public CType ConvertType(ITrophyType type) {
            if (this.typeNames.TryGetValue(type, out var ctype)) {
                return ctype;
            }

            if (type.IsBoolType) {
                return CType.NamedType("trophy_bool");
            }
            else if (type.IsIntType) {
                return CType.NamedType("trophy_int");
            }
            else if (type.IsVoidType) {
                return CType.NamedType("trophy_void");
            }
            else if (type.AsFixedArrayType().TryGetValue(out var fixedArrayType)) {
                return this.ConvertType(new ArrayType(fixedArrayType.ElementType, fixedArrayType.IsReadOnly));
            }
            else if (type.AsArrayType().TryGetValue(out var arrayType)) {
                if (arrayType.IsReadOnly) {
                    return this.ConvertType(new ArrayType(arrayType.ElementType, false));
                }

                return this.MakeArrayType(arrayType);
            }
            else if (type.AsVariableType().TryGetValue(out var type2)) {
                return CType.Pointer(ConvertType(type2.InnerType));
            }
            else if (type.AsNamedType().TryGetValue(out var path)) {
                return CType.NamedType("$" + path);
            }
            else if (type.AsFunctionType().TryGetValue(out var funcType)) {
                return this.MakeFunctionType(funcType);
            }
            else {
                throw new Exception();
            }
        }

        private CType MakeFunctionType(FunctionType funcType) {
            var returnType = this.ConvertType(funcType.ReturnType);
            var parTypes = funcType
                .ParameterTypes
                .Select((x, i) => new CParameter(this.ConvertType(x), "arg" + i))
                .Prepend(new CParameter(CType.VoidPointer, "environment"))
                .ToArray();

            var pointerName = "FuncType_" + typeCounter++;
            var structName = "ClosureType_" + typeCounter++;

            var members = new[] {
                new CParameter(CType.VoidPointer, "environment"), 
                new CParameter(CType.NamedType(pointerName), "function")
            };

            if (funcType.ReturnType.IsVoidType) {
                this.WriteDeclaration2(CDeclaration.FunctionPointer(pointerName, parTypes));
                this.WriteDeclaration2(CDeclaration.EmptyLine());
            }
            else {
                this.WriteDeclaration2(CDeclaration.FunctionPointer(pointerName, returnType, parTypes));
                this.WriteDeclaration2(CDeclaration.EmptyLine());
            }            

            this.WriteDeclaration1(CDeclaration.StructPrototype(structName));
            this.WriteDeclaration1(CDeclaration.EmptyLine());

            this.WriteDeclaration2(CDeclaration.Struct(structName, members));
            this.WriteDeclaration2(CDeclaration.EmptyLine());

            return this.typeNames[funcType] = CType.NamedType(structName);
        }

        private CType MakeArrayType(ArrayType arrayType) {
            var name = "ArrayType" + typeCounter++;
            var innerType = CType.Pointer(this.ConvertType(arrayType.ElementType));
            var members = new[] {
                    new CParameter(CType.NamedType("trophy_int"), "size"), new CParameter(innerType, "data")
                };

            this.WriteDeclaration1(CDeclaration.StructPrototype(name));
            this.WriteDeclaration1(CDeclaration.EmptyLine());

            this.WriteDeclaration2(CDeclaration.Struct(name, members));
            this.WriteDeclaration2(CDeclaration.EmptyLine());

            return this.typeNames[arrayType] = CType.NamedType(name);
        }
    }
}