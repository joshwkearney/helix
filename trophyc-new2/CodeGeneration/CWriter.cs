using System.Text;
using Trophy.Analysis;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Aggregates;
using Trophy.Features.Functions;
using Trophy.Parsing;

namespace Trophy.CodeGeneration {
    public class CWriter : ITypesObserver, INamesObserver, ISyntaxNavigator {
        private readonly ITypesObserver types;
        private readonly INamesObserver names;

        private int tempCounter = 0;
        private readonly Dictionary<TrophyType, CType> typeNames = new();
        private readonly Dictionary<IdentifierPath, string> tempNames = new();

        private readonly StringBuilder decl1Sb = new StringBuilder();
        private readonly StringBuilder decl2Sb = new StringBuilder();
        private readonly StringBuilder decl3Sb = new StringBuilder();

        public IdentifierPath CurrentScope { get; }

        public CWriter(INamesObserver names, ITypesObserver types) {
            this.names = names;
            this.types = types;

            this.decl1Sb.AppendLine("#include \"include/trophy.h\"");
            this.decl1Sb.AppendLine();
        }

        public override string ToString() {
            return new StringBuilder()
                .Append(this.decl1Sb)
                .Append(this.decl2Sb)
                .Append(this.decl3Sb)
                .ToString();
        }

        public string GetVariableName() {
            return "$t_" + this.tempCounter++;
        }

        public string GetVariableName(IdentifierPath path) {
            if (path.Segments.Count == 1) {
                return path.Segments.First();
            }

            if (!this.tempNames.TryGetValue(path, out var value)) {
                value = this.tempNames[path] = path.Segments.Last() + "_" + this.tempCounter++;
            }

            return value;
        }

        public void WriteDeclaration1(CDeclaration decl) {
            decl.Write(0, this.decl1Sb);
        }

        public void WriteDeclaration2(CDeclaration decl) {
            decl.Write(0, this.decl2Sb);
        }

        public void WriteDeclaration3(CDeclaration decl) {
            decl.Write(0, this.decl3Sb);
        }

        // Interface wrappers
        public CType ConvertType(TrophyType type) {
            if (this.typeNames.TryGetValue(type, out var ctype)) {
                return ctype;
            }

            if (type == PrimitiveType.Bool) {
                return CType.NamedType("unsigned int");
            }
            else if (type == PrimitiveType.Int) {
                return CType.NamedType("unsigned int");
            }
            else if (type == PrimitiveType.Void) {
                return CType.NamedType("unsigned int");
            }
            else if (type.AsPointerType().TryGetValue(out var type2)) {
                return CType.Pointer(ConvertType(type2.ReferencedType));
            }
            else if (type.AsNamedType().TryGetValue(out var type3)) {
                return CType.NamedType(string.Join("$", type3.FullName.Segments));
            }
            //else if (type.AsFunctionType().TryGetValue(out var funcType)) {
            //    return this.MakeFunctionType(funcType);
            //}
            else {
                throw new Exception();
            }
        }

        public TrophyType GetReturnType(ISyntaxTree tree) {
            return this.types.GetReturnType(tree);
        }

        public FunctionSignature GetFunction(IdentifierPath path) {
            return this.types.GetFunction(path);
        }

        public VariableSignature GetVariable(IdentifierPath path) {
            return this.types.GetVariable(path);
        }

        public AggregateSignature GetAggregate(IdentifierPath path) {
            return this.types.GetAggregate(path);
        }

        public bool IsReserved(IdentifierPath path) {
            return this.types.IsReserved(path);
        }

        public Option<NameTarget> TryResolveName(IdentifierPath path) {
            return this.names.TryResolveName(path);
        }

        public void PushScope(IdentifierPath scope) { }

        public void PopScope() { }
    }
}