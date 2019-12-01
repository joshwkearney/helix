using Attempt16.Analysis;
using Attempt16.Types;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt16.Generation {
    public class TypeGenerator : ITypeVisitor<CType> {
        private readonly Dictionary<ILanguageType, string> typeNames = new Dictionary<ILanguageType, string>();
        private readonly List<string> code = new List<string>();
        private readonly Scope scope;

        public IReadOnlyDictionary<ILanguageType, string> TypeNames => this.typeNames;

        public IReadOnlyList<string> Code => this.code;

        public TypeGenerator(Scope scope) {
            this.scope = scope;
        }

        public CType VisitFunctionType(SingularFunctionType type) {
            if (this.typeNames.TryGetValue(type, out string funcName)) {
                return new CType() {
                    CTypeName = funcName,
                    HeaderLines = ImmutableList<string>.Empty
                };
            }

            funcName = type.ScopePath.Segments.Last();
            this.typeNames[type] = funcName;

            var writer = new CWriter();
            var returnType = this.VisitIdenfitierPath(type.ReturnTypePath);
            var pars = new List<(string type, string name)>();

            writer.Append(returnType);

            foreach (var (partype, name) in type.Parameters) {
                var ctype = this.VisitIdenfitierPath(partype);

                writer.Append(ctype);
                pars.Add((ctype.CTypeName, name));
            }

            writer.ForwardDeclaration(funcName, returnType.CTypeName, pars);

            return new CType() {
                CTypeName = funcName,
                HeaderLines = writer.HeaderCode
            };
        }

        public CType VisitIntType(IntType type) {
            this.typeNames[type] = "int";

            return new CType() {
                CTypeName = "int",
                HeaderLines = ImmutableList<string>.Empty
            };
        }

        public CType VisitVariableType(VariableType type) {
            var inner = type.TargetType.Accept(this);

            string name = inner.CTypeName + "*";
            this.typeNames[type] = name;

            return new CType() {
                CTypeName = name,
                HeaderLines = inner.HeaderLines
            };
        }

        public CType VisitStructType(SingularStructType type) {
            if (this.typeNames.TryGetValue(type, out string name)) {
                return new CType() {
                    CTypeName = name,
                    HeaderLines = ImmutableList<string>.Empty
                };
            }

            var writer = new CWriter();
            var structWriter = new CWriter();

            structWriter.HeaderLine("struct " + type.Name + " {");

            if (!type.Members.Any()) {
                structWriter.HeaderLines(CWriter.Indent("char _;"));
            }
            
            foreach (var member in type.Members) {
                var ctype = this.VisitIdenfitierPath(member.TypePath);

                structWriter.Append(ctype);
                structWriter.HeaderLines(CWriter.Indent(ctype.CTypeName + " " + member.Name + ";"));
            }

            structWriter.HeaderLine("};");
            structWriter.HeaderLine();

            writer.Append(structWriter);
            this.typeNames[type] = type.Name;

            return new CType() {
                CTypeName = type.Name,
                HeaderLines = writer.HeaderCode
            };
        }

        public CType VisitVoidType(VoidType type) {
            this.typeNames[type] = "char";

            return new CType() {
                CTypeName = "char",
                HeaderLines = ImmutableList<string>.Empty
            };
        }

        public CType VisitIdenfitierPath(IdentifierPath path) {
            if (path.Segments.Count == 0) {
                throw new System.Exception();
            }

            if (path.Segments.First() == "%var") {
                var code = this.VisitIdenfitierPath(new IdentifierPath(path.Segments.Skip(1)));
                code.CTypeName += "*";

                return code;
            }
            else {
                return new CType() {
                    CTypeName = path.Segments.Last(),
                    HeaderLines = ImmutableList<string>.Empty
                };
            }
        }
    }
}