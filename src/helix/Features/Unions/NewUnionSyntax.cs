using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;

namespace Helix.Features.Unions {
    public class NewUnionSyntax : ISyntaxTree {
        private static int tempCounter = 0;

        private readonly HelixType unionType;
        private readonly UnionType sig;
        private readonly IReadOnlyList<string> names;
        private readonly IReadOnlyList<ISyntaxTree> values;
        private readonly IdentifierPath tempPath;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure { get; }

        public NewUnionSyntax(TokenLocation loc, HelixType unionType, UnionType sig,
                              IReadOnlyList<string> names, IReadOnlyList<ISyntaxTree> values,
                              IdentifierPath path) {
            this.Location = loc;
            this.unionType = unionType;
            this.sig = sig;
            this.names = names;
            this.values = values;
            this.tempPath = path;

            this.IsPure = this.values.All(x => x.IsPure);
        }

        public NewUnionSyntax(TokenLocation loc, HelixType unionType, UnionType sig,
                              IReadOnlyList<string> names, IReadOnlyList<ISyntaxTree> values)
            : this(loc, unionType, sig, names, values, new IdentifierPath("$union" + tempCounter++)) { }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            if (this.names.Count > 1 || this.values.Count > 1) {
                throw new TypeException(
                    this.Location,
                    "Invalid Union Initialization",
                    "Union initializers must have at most one argument.");
            }

            string name;
            if (this.names.Count == 0 || this.names[0] == null) {
                name = this.sig.Members[0].Name;
            }
            else {
                name = this.names[0];
            }

            var mem = this.sig.Members.FirstOrDefault(x => x.Name == name);
            if (mem == null) {
                throw new TypeException(
                    this.Location,
                    "Invalid Union Initialization",
                    $"The member '{name}' does not exist in the "
                        + $"union type '{this.unionType}'");
            }

            ISyntaxTree value;
            if (this.values.Count == 0) {
                if (!mem.Type.HasDefaultValue(types)) {
                    throw new TypeException(
                    this.Location,
                    "Invalid Union Initialization",
                    $"The union member '{name}' does not have a default value. " 
                    + "Please supply an explicit value or initialize the union " 
                    + "with a different member.");
                }

                value = new VoidLiteral(this.Location)
                    .CheckTypes(types)
                    .ToRValue(types)
                    .UnifyTo(mem.Type, types);
            }
            else {
                value = this.values[0]
                    .CheckTypes(types)
                    .ToRValue(types)
                    .UnifyTo(mem.Type, types);
            }

            var result = new NewUnionSyntax(
                this.Location, 
                this.unionType,
                this.sig, 
                new[] { name }, 
                new[] { value },
                types.Scope.Append(this.tempPath));
            
            SyntaxTagBuilder.AtFrame(types)
                .WithChildren(value)
                .WithReturnType(this.unionType)
                .BuildFor(result);

            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw TypeException.RValueRequired(this.Location);
            }

            return this;
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            var name = this.names[0];
            var value = this.values[0].GenerateCode(types, writer);

            var unionStructType = writer.ConvertType(this.unionType, types);
            var unionUnionType = new CNamedType(unionStructType.WriteToC() + "_$Union");
            var index = this.sig.Members.IndexOf(x => x.Name == name);

            return new CCompoundExpression() {
                Type = unionStructType,
                MemberNames = new[] { "tag", "data" },
                Arguments = new ICSyntax[] { 
                    new CIntLiteral(index),
                    new CCompoundExpression() {
                        MemberNames = new[] { name },
                        Arguments = new[] { value }
                    }
                },
            };
        }
    }
}