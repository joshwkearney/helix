using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;

namespace Helix.Features.Aggregates {
    public class NewUnionSyntax : ISyntaxTree {
        private static int tempCounter = 0;

        private readonly StructSignature sig;
        private readonly IReadOnlyList<string> names;
        private readonly IReadOnlyList<ISyntaxTree> values;
        private readonly IdentifierPath tempPath;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure { get; }

        public NewUnionSyntax(
            TokenLocation loc, 
            StructSignature sig,
            IReadOnlyList<string> names,
            IReadOnlyList<ISyntaxTree> values,
            IdentifierPath tempPath) {

            this.Location = loc;
            this.sig = sig;
            this.names = names;
            this.values = values;
            this.tempPath = tempPath;

            this.IsPure = this.values.All(x => x.IsPure);
        }

        public NewUnionSyntax(TokenLocation loc, StructSignature sig,
                              IReadOnlyList<string> names, IReadOnlyList<ISyntaxTree> values)
            : this(loc, sig, names, values, loc.Scope.Append("$new_union_" + tempCounter++)) { }

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

            var unionType = new NominalType(this.sig.Path, NominalTypeKind.Union);

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
                        + $"union type '{unionType}'");
            }

            ISyntaxTree value;
            if (this.values.Count == 0) {
                if (!mem.Type.HasDefaultValue(types)) {
                    throw new TypeException(
                    this.Location,
                    "Invalid Struct Initialization",
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
                this.sig, 
                new[] { name }, 
                new[] { value },
                this.tempPath);

            result.SetReturnType(unionType, types);
            result.SetCapturedVariables(value, types);
            result.SetPredicate(value, types);

            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw TypeException.RValueRequired(this.Location);
            }

            return this;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            if (this.IsFlowAnalyzed(flow)) {
                return;
            }

            var name = this.names[0];
            var value = this.values[0];

            value.AnalyzeFlow(flow);

            var valueBundle = value.GetLifetimes(flow);
            var bundleDict = new Dictionary<IdentifierPath, LifetimeBounds>();

            var lifetime = new ValueLifetime(
                this.tempPath.ToVariablePath(),
                LifetimeRole.Alias,
                LifetimeOrigin.TempValue);

            foreach (var (relPath, _) in value.GetReturnType(flow).GetMembers(flow)) {
                var valueLifetime = valueBundle[relPath].ValueLifetime;

                flow.LifetimeGraph.AddStored(valueLifetime, lifetime, null);
            }

            bundleDict[new IdentifierPath()] = new LifetimeBounds(lifetime);
            this.SetLifetimes(new LifetimeBundle(bundleDict), flow);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            var name = this.names[0];
            var value = this.values[0].GenerateCode(types, writer);

            var unionStructType = writer.ConvertType(new NominalType(this.sig.Path, NominalTypeKind.Union));
            var unionUnionType = new CNamedType(writer.GetVariableName(this.sig.Path) + "$union");
            var index = this.sig.Members.IndexOf(x => x.Name == name);

            var tempName = writer.GetVariableName(this.tempPath);
            var tempDecl = new CVariableDeclaration() {
                Name = tempName,
                Type = unionUnionType
            };

            var tempAssign = new CAssignment() {
                Left = new CMemberAccess() {
                    Target = new CVariableLiteral(tempName),
                    MemberName = name
                },
                Right = value
            };

            writer.WriteComment($"Line {this.Location.Line}: Union literal");
            writer.WriteStatement(tempDecl);
            writer.WriteStatement(tempAssign);

            return new CCompoundExpression() {
                Type = unionStructType,
                Arguments = new ICSyntax[] { 
                    new CIntLiteral(index),
                    new CVariableLiteral(tempName)
                },
            };
        }
    }
}