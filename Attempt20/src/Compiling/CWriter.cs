using System;
using System.Collections.Generic;
using System.Text;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;

namespace Attempt20.Compiling {
    public class CWriter : ICWriter {
        private bool regionHeadersGenerated = false;

        private int arrayTypeCounter = 0;
        private readonly Dictionary<ArrayType, CType> arrayTypeNames = new Dictionary<ArrayType, CType>();

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
            this.WriteForwardDeclaration(CDeclaration.StructPrototype("$Region"));

            // Region alloc forward declaration
            var regionPointerType = CType.Pointer(CType.NamedType("$Region"));
            var decl = CDeclaration.FunctionPrototype(CType.VoidPointer, "$region_alloc", new[] {
                    new CParameter(regionPointerType, "region"), new CParameter(CType.Integer, "bytes")
                });

            this.WriteForwardDeclaration(decl);

            // Region create forward declaration
            var decl2 = CDeclaration.FunctionPrototype(regionPointerType, "$region_create", new CParameter[0]);

            this.WriteForwardDeclaration(decl2);

            // Region delete forward declaration
            var decl3 = CDeclaration.FunctionPrototype("$region_delete", new[] {
                    new CParameter(regionPointerType, "region")
                });

            this.WriteForwardDeclaration(decl3);
            this.WriteForwardDeclaration(CDeclaration.EmptyLine());

            this.regionHeadersGenerated = true;
        }

        public void WriteForwardDeclaration(CDeclaration decl) {
            decl.WriteToC(0, this.forwardDeclSb);
        }

        public CType ConvertType(TrophyType type) {
            if (type.IsBoolType || type.IsIntType || type.IsVoidType || type.AsSingularFunctionType().Any()) {
                return CType.Integer;
            }
            else if (type.AsArrayType().TryGetValue(out var arrayType)) {
                return this.MakeArrayType(arrayType);
            }
            else if (type.AsFixedArrayType().TryGetValue(out var fixedArrayType)) {
                return this.MakeArrayType(new ArrayType(fixedArrayType.ElementType));
            }
            else if (type.AsVariableType().TryGetValue(out var type2)) {
                return CType.Pointer(ConvertType(type2.InnerType));
            }
            else if (type.AsNamedType().TryGetValue(out var path)) {
                return CType.NamedType(path.ToString());
            }
            else {
                throw new Exception();
            }
        }

        private CType MakeArrayType(ArrayType arrayType) {
            if (this.arrayTypeNames.TryGetValue(arrayType, out var ctype)) {
                return ctype;
            }

            var name = "$ArrayType" + arrayTypeCounter++;
            var innerType = CType.Pointer(this.ConvertType(arrayType.ElementType));
            var members = new[] {
                    new CParameter(CType.Integer, "size"), new CParameter(innerType, "data")
                };

            this.WriteForwardDeclaration(CDeclaration.StructPrototype(name));
            this.WriteForwardDeclaration(CDeclaration.EmptyLine());

            this.WriteDeclaration(CDeclaration.Struct(name, members));
            this.WriteDeclaration(CDeclaration.EmptyLine());

            return this.arrayTypeNames[arrayType] = CType.NamedType(name);
        }
    }
}
