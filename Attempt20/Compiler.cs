using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Attempt20.CodeGeneration;
using Attempt20.Features.Arrays;
using Attempt20.Features.Containers;
using Attempt20.Features.FlowControl;
using Attempt20.Features.Primitives;

namespace Attempt20 {
    public class Compiler {
        private readonly string input;

        public Compiler(string input) {
            this.input = input.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        public string Compile() {
            try {
                return this.CompileHelper();
            }
            catch (CompilerException ex) {
                // Calculate line number and start index on that line
                var lines = this.input.Split(new[] { "\n" }, StringSplitOptions.None);
                int line = 1 + this.input.Substring(0, ex.Location.StartIndex).Count(x => x == '\n');
                int start = ex.Location.StartIndex - lines.Take(line - 1).Select(x => x.Length + 1).Sum();
                int minline = Math.Max(1, line - 2);
                int maxline = Math.Min(lines.Length, line + 2);

                // Calculate line number padding
                int padding = maxline.ToString().Length;
                var format = $"{{0,{padding}:{new string(Enumerable.Repeat('#', padding).ToArray())}}}|";
                
                // Print preamble
                var message = $"Unhandled compilation exception: {ex.Title}\n";
                message += ex.Message + "\n";
                message += $"at 'program.txt' line {line} pos {start}\n";
                message += "\n";

                // Print the previous two lines
                for (int i = minline; i <= line; i++) {
                    message += string.Format(format, i) + lines[i - 1] + "\n";
                }

                // Calculate the underlining
                var length = Math.Min(lines[line - 1].Length - start, ex.Location.Length);
                var spaces = new string(Enumerable.Repeat(' ', start + padding + 1).ToArray());
                var arrows = new string(Enumerable.Repeat('^', length).ToArray());

                // Print underlining
                message += spaces + arrows + "\n";

                // Print the following two lines
                for (int i = line + 1; i <= maxline; i++) {
                    message += string.Format(format, i) + lines[i - 1] + "\n";
                }

                throw new CompilerException(ex.Location, ex.Title, message);
            }
        }

        private string CompileHelper() {
            var lexer = new Lexer(this.input);
            var parser = new Parser(lexer.GetTokens());
            var names = new NamesRecorder();
            var types = new TypesRecorder();
            var writer = new CWriter();
            var declarations = parser.Parse();

            // Declare the heap and the stack for all code
            names.DeclareGlobalName(new IdentifierPath("heap"), NameTarget.Region);
            names.DeclareGlobalName(new IdentifierPath("stack"), NameTarget.Region);

            foreach (var decl in declarations) {
                decl.DeclareNames(names);
            }

            foreach (var decl in declarations) {
                decl.ResolveNames(names);
            }

            foreach (var decl in declarations) {
                decl.DeclareTypes(names, types);
            }

            foreach (var decl in declarations) {
                var tree = decl.ResolveTypes(names, types);
                tree.GenerateCode(writer);
            }

            return writer.ToString();
        }

        private class CWriter : ICDeclarationWriter {
            private bool regionHeadersGenerated = false;

            private int arrayTypeCounter = 0;
            private readonly Dictionary<ArrayType, CType> arrayTypeNames = new Dictionary<ArrayType, CType>();

            private readonly StringBuilder forwardDeclSb = new StringBuilder();
            private readonly StringBuilder declSb = new StringBuilder();

            public CWriter() {
                this.forwardDeclSb.AppendLine("#include <stdlib.h>");
                this.forwardDeclSb.AppendLine("#include <stdio.h>");
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

            public CType ConvertType(LanguageType type) {
                if (type.IsBoolType || type.IsIntType || type.IsVoidType || type.AsSingularFunctionType().Any()) {
                    return CType.Integer;
                }
                else if (type.AsArrayType().TryGetValue(out var arrayType)) {
                    return this.MakeArrayType(arrayType);
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

        private class TypesRecorder : ITypeRecorder {
            private readonly Dictionary<IdentifierPath, VariableInfo> variables = new Dictionary<IdentifierPath, VariableInfo>();
            private readonly Dictionary<IdentifierPath, FunctionSignature> functions = new Dictionary<IdentifierPath, FunctionSignature>();
            private readonly Dictionary<IdentifierPath, StructSignature> structs = new Dictionary<IdentifierPath, StructSignature>();

            public void DeclareVariable(IdentifierPath path, VariableInfo info) {
                this.variables[path] = info;
            }

            public void DeclareFunction(IdentifierPath path, FunctionSignature sig) {
                this.functions[path] = sig;
            }

            public void DeclareStruct(IdentifierPath path, StructSignature sig) {
                this.structs[path] = sig;
            }

            public IOption<FunctionSignature> TryGetFunction(IdentifierPath path) {
                return this.functions.GetValueOption(path);
            }

            public IOption<VariableInfo> TryGetVariable(IdentifierPath path) {
                return this.variables.GetValueOption(path);
            }

            public IOption<StructSignature> TryGetStruct(IdentifierPath path) {
                return structs.GetValueOption(path);
            }

            public IOption<ITypeCheckedSyntax> TryUnifyTo(ITypeCheckedSyntax target, LanguageType newType) {
                if (target.ReturnType == newType) {
                    return Option.Some(target);
                }

                if (target.ReturnType.IsVoidType) {
                    if (newType.IsIntType) {
                        return Option.Some(new VoidToPrimitiveAdapter() { Target = target, ReturnType = LanguageType.Integer });
                    }
                    else if (newType.IsBoolType) {
                        return Option.Some(new VoidToPrimitiveAdapter() { Target = target, ReturnType = LanguageType.Boolean });
                    }
                    else if (newType.AsSingularFunctionType().Any()) {
                        return Option.Some(new VoidToPrimitiveAdapter() { Target = target, ReturnType = newType });
                    }
                    else if (newType.AsArrayType().TryGetValue(out var arrayType)) {
                        return Option.Some(new VoidToArrayAdapter() { Target = target, ReturnType = arrayType });
                    }
                    else if (newType.AsNamedType().TryGetValue(out var path)) {
                        if (this.TryGetStruct(path).TryGetValue(out var sig) && newType.HasDefaultValue(this)) {
                            return Option.Some(new VoidToStructAdapter(target, sig, newType, this));
                        }
                    }
                }
                else if (target.ReturnType.IsBoolType) {
                    if (newType.IsIntType) {
                        return Option.Some(new BoolToIntAdapter() { Target = target });
                    }
                }
                else if (target.ReturnType.AsFixedArrayType().TryGetValue(out var fixedArrayType)) {
                    if (newType.AsArrayType().TryGetValue(out var arrayType) && fixedArrayType.ElementType == arrayType.ElementType) {
                        return Option.Some(new FixedArrayToArrayAdapter() { ReturnType = newType, Target = target });
                    }
                }


                return Option.None<ITypeCheckedSyntax>();
            }
        };

        private class NamesRecorder : INameRecorder {
            private readonly Stack<IdentifierPath> scopes = new Stack<IdentifierPath>();
            private readonly Stack<IdentifierPath> regions = new Stack<IdentifierPath>();


            private readonly Dictionary<IdentifierPath, NameTarget> globalNames
                        = new Dictionary<IdentifierPath, NameTarget>();

            private readonly Stack<Dictionary<IdentifierPath, NameTarget>> localNames
                = new Stack<Dictionary<IdentifierPath, NameTarget>>();

            public IdentifierPath CurrentScope => this.scopes.Peek();

            public IdentifierPath CurrentRegion => this.regions.Peek();

            public NamesRecorder() {
                this.scopes.Push(new IdentifierPath());
                this.localNames.Push(new Dictionary<IdentifierPath, NameTarget>());
            }

            public void DeclareGlobalName(IdentifierPath path, NameTarget target) {
                this.globalNames[path] = target;
            }

            public void DeclareLocalName(IdentifierPath path, NameTarget target) {
                this.localNames.Peek()[path] = target;
            }

            public void PopScope() {
                this.scopes.Pop();
                this.localNames.Pop();
            }

            public void PushScope(IdentifierPath newScope) {
                this.scopes.Push(newScope);
                this.localNames.Push(new Dictionary<IdentifierPath, NameTarget>());
            }

            public bool TryFindName(string name, out NameTarget target, out IdentifierPath path) {
                var scope = this.CurrentScope;

                while (true) {
                    path = scope.Append(name);

                    if (this.TryGetName(path, out target)) {
                        return true;
                    }

                    if (!scope.Segments.Any()) {
                        path = new IdentifierPath(name);
                        return this.TryGetName(path, out target);
                    }

                    scope = scope.Pop();
                }
            }

            public bool TryGetName(IdentifierPath name, out NameTarget target) {
                foreach (var frame in this.localNames) {
                    if (frame.TryGetValue(name, out target)) {
                        return true;
                    }
                }

                return this.globalNames.TryGetValue(name, out target);
            }

            public void PushRegion(IdentifierPath newRegion) {
                this.regions.Push(newRegion);
            }

            public void PopRegion() {
                this.regions.Pop();
            }

            public LanguageType ResolveTypeNames(LanguageType type, TokenLocation loc) {
                if (type.IsBoolType || type.IsIntType || type.IsVoidType) {
                    return type;
                }
                else if (type.AsSingularFunctionType().Any()) {
                    return type;
                }
                else if (type.AsArrayType().TryGetValue(out var arrayType)) {
                    return new ArrayType(this.ResolveTypeNames(arrayType.ElementType, loc));
                }
                else if (type.AsVariableType().TryGetValue(out var varType)) {
                    return new VariableType(this.ResolveTypeNames(varType.InnerType, loc));
                }
                else if (type.AsNamedType().TryGetValue(out var name)) {
                    if (!this.TryFindName(name.ToString(), out var target, out var path)) {
                        throw TypeCheckingErrors.TypeUndefined(loc, name.ToString());
                    }

                    if (target == NameTarget.Function) {
                        return new SingularFunctionType(path);
                    }
                    else if (target == NameTarget.Struct) {
                        return LanguageType.FromPath(path);
                    }
                    else {
                        throw TypeCheckingErrors.TypeUndefined(loc, name.ToString());
                    }
                }
                else {
                    throw new Exception();
                }
            }
        }
    }
}
