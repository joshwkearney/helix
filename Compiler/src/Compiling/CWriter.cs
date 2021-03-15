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

        private readonly StringBuilder forwardDeclSb = new StringBuilder();
        private readonly StringBuilder declSb = new StringBuilder();

        public CWriter() {
            this.forwardDeclSb.AppendLine("#include <stdlib.h>");
            this.forwardDeclSb.AppendLine("#include <stdio.h>");
            this.forwardDeclSb.AppendLine("#include <string.h>");
            this.forwardDeclSb.AppendLine();
        }

        public void WriteDeclaration(CDeclaration decl) {
            decl.WriteToC(0, this.declSb);
        }

        public override string ToString() {
            return new StringBuilder().Append(this.forwardDeclSb).Append(this.declSb).ToString();
        }

        public void RequireRegions() {
            if (this.regionHeadersGenerated) {
                return;
            }

            // Region struct forward declaration
            this.WriteForwardDeclaration(CDeclaration.StructPrototype("Region"));

            // Region alloc forward declaration
            var regionPointerType = CType.Pointer(CType.NamedType("Region"));
            var decl = CDeclaration.FunctionPrototype(CType.VoidPointer, "region_alloc", false, new[] {
                    new CParameter(regionPointerType, "region"), new CParameter(CType.Integer, "bytes")
                });

            this.WriteForwardDeclaration(decl);

            // Region create forward declaration
            var decl2 = CDeclaration.FunctionPrototype(regionPointerType, "region_create", false, new CParameter[0]);

            this.WriteForwardDeclaration(decl2);

            // Region delete forward declaration
            var decl3 = CDeclaration.FunctionPrototype("region_delete", false, new[] {
                    new CParameter(regionPointerType, "region")
                });

            this.WriteForwardDeclaration(decl3);
            this.WriteForwardDeclaration(CDeclaration.EmptyLine());

            this.regionHeadersGenerated = true;
        }

        public void WriteForwardDeclaration(CDeclaration decl) {
            decl.WriteToC(0, this.forwardDeclSb);
        }

        public CType ConvertType(ITrophyType type) {
            if (type.IsBoolType || type.IsIntType || type.IsVoidType || type.AsSingularFunctionType().Any()) {
                return CType.Integer;
            }
            else if (type.AsFixedArrayType().TryGetValue(out var fixedArrayType)) {
                return this.MakeArrayType(new ArrayType(fixedArrayType.ElementType, fixedArrayType.IsReadOnly));
            }
            else if (type.AsArrayType().TryGetValue(out var arrayType)) {
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
            if (this.typeNames.TryGetValue(funcType, out var ctype)) {
                return ctype;
            }

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

            this.WriteForwardDeclaration(CDeclaration.FunctionPointer(pointerName, returnType, parTypes));
            this.WriteForwardDeclaration(CDeclaration.EmptyLine());

            this.WriteForwardDeclaration(CDeclaration.StructPrototype(structName));
            this.WriteForwardDeclaration(CDeclaration.EmptyLine());

            this.WriteForwardDeclaration(CDeclaration.Struct(structName, members));
            this.WriteForwardDeclaration(CDeclaration.EmptyLine());

            return this.typeNames[funcType] = CType.NamedType(structName);
        }


        private CType MakeArrayType(ArrayType arrayType) {
            if (this.typeNames.TryGetValue(arrayType, out var ctype)) {
                return ctype;
            }

            var name = "ArrayType" + typeCounter++;
            var innerType = CType.Pointer(this.ConvertType(arrayType.ElementType));
            var members = new[] {
                    new CParameter(CType.Integer, "size"), new CParameter(innerType, "data")
                };

            this.WriteForwardDeclaration(CDeclaration.StructPrototype(name));
            this.WriteForwardDeclaration(CDeclaration.EmptyLine());

            this.WriteForwardDeclaration(CDeclaration.Struct(name, members));
            this.WriteForwardDeclaration(CDeclaration.EmptyLine());

            return this.typeNames[arrayType] = CType.NamedType(name);
        }
    }
}