using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;

namespace Trophy.Compiling {
    public class CWriter : ICWriter {
        private bool regionHeadersGenerated = false;

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

        public void RequireRegions() {
            if (this.regionHeadersGenerated) {
                return;
            }

            // Region struct forward declaration
            this.WriteDeclaration1(CDeclaration.StructPrototype("Region"));
            this.WriteDeclaration1(CDeclaration.EmptyLine());

            // Region alloc forward declaration
            var regionPointerType = CType.Pointer(CType.NamedType("Region"));
            var decl = CDeclaration.FunctionPrototype(CType.VoidPointer, "region_alloc", false, new[] {
                new CParameter(regionPointerType, "region"), new CParameter(CType.Integer, "bytes")
            });

            this.WriteDeclaration2(decl);

            // Region create forward declaration
            var decl2 = CDeclaration.FunctionPrototype(regionPointerType, "region_create_parent", false, new CParameter[0]);
            var decl4 = CDeclaration.FunctionPrototype(regionPointerType, "region_create_child", false, new[] { 
                new CParameter(regionPointerType, "region")
            });

            this.WriteDeclaration2(decl2);
            this.WriteDeclaration2(decl4);

            // Region delete forward declaration
            var decl3 = CDeclaration.FunctionPrototype("region_delete", false, new[] {
                    new CParameter(regionPointerType, "region")
                });

            this.WriteDeclaration2(decl3);

            // Region get panic buffer forward declaration
            var decl5 = CDeclaration.FunctionPrototype(CType.VoidPointer, "region_get_panic_buffer", false, new[] {
                    new CParameter(regionPointerType, "region")
                });

            this.WriteDeclaration2(decl5);

            this.WriteDeclaration2(CDeclaration.EmptyLine());

            this.regionHeadersGenerated = true;
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

            if (type.IsBoolType || type.IsIntType || type.IsVoidType || type.AsSingularFunctionType().Any()) {
                return CType.Integer;
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

            this.WriteDeclaration2(CDeclaration.FunctionPointer(pointerName, returnType, parTypes));
            this.WriteDeclaration2(CDeclaration.EmptyLine());

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
                    new CParameter(CType.Integer, "size"), new CParameter(innerType, "data")
                };

            this.WriteDeclaration1(CDeclaration.StructPrototype(name));
            this.WriteDeclaration1(CDeclaration.EmptyLine());

            this.WriteDeclaration2(CDeclaration.Struct(name, members));
            this.WriteDeclaration2(CDeclaration.EmptyLine());

            return this.typeNames[arrayType] = CType.NamedType(name);
        }
    }
}