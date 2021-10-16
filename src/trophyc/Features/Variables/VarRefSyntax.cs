using System.Collections.Immutable;
using System.Linq;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy.Features.Variables {
    public class VarRefSyntaxA : ISyntaxA {
        private readonly string name;
        private readonly ISyntaxA assign;
        private readonly bool isreadonly;

        public TokenLocation Location { get; }

        public VarRefSyntaxA(TokenLocation loc, string name, ISyntaxA assign, bool isreadonly) {
            this.Location = loc;
            this.name = name;
            this.assign = assign;
            this.isreadonly = isreadonly;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            var assign = this.assign.CheckNames(names);
            var path = names.Context.Scope.Append(this.name);

            // Make sure we're not shadowing another variable
            if (names.TryFindName(this.name, out _, out _)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.name);
            }

            // Declare this variable
            names.DeclareName(path, NameTarget.Variable, IdentifierScope.LocalName);

            return new VarRefSyntaxB(
                loc: this.Location, 
                path: path, 
                id: names.GetNewVariableId(),
                region: names.Context.Region, 
                assign: assign,
                this.isreadonly);
        }
    }

    public class VarRefSyntaxB : ISyntaxB {
        private readonly IdentifierPath path;
        private readonly IdentifierPath region;
        private readonly int id;
        private readonly ISyntaxB assign;
        private readonly bool isreadonly;

        public TokenLocation Location { get; }

        public IImmutableSet<VariableUsage> VariableUsage {
            get => this.assign
                .VariableUsage
                .Where(x => x.VariablePath != this.path)
                .ToImmutableHashSet();
        }

        public VarRefSyntaxB(TokenLocation loc, IdentifierPath path, int id, IdentifierPath region, ISyntaxB assign, bool isreadonly) {
            this.Location = loc;
            this.path = path;
            this.id = id;
            this.assign = assign;
            this.region = region;
            this.isreadonly = isreadonly;
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            var assign = this.assign.CheckTypes(types);

            // If this is a var declaration and the type is a dependent type (singular int type, 
            // singular function type, or fixed array type, convert it to the more abstract form
            // so the type system doesn't get in the way
            if (!this.isreadonly) {
                if (assign.ReturnType.AsFixedArrayType().TryGetValue(out var fixedArrayType)) {
                    assign = types.TryUnifyTo(assign, new ArrayType(fixedArrayType.ElementType, fixedArrayType.IsReadOnly)).GetValue();
                }
                else if (assign.ReturnType.AsSingularFunctionType().TryGetValue(out var singFuncType)) {
                    var sig = types.TryGetFunction(singFuncType.FunctionPath).GetValue();
                    var pars = sig.Parameters.Select(x => x.Type).ToArray();

                    assign = types.TryUnifyTo(assign, new FunctionType(sig.ReturnType, pars)).GetValue();
                }
            }

            var info = new VariableInfo(
                name: this.path.Segments.Last(),
                innerType: new VarRefType(assign.ReturnType, this.isreadonly),
                source: VariableSource.Local,
                kind: this.isreadonly ? VariableKind.RefVariable : VariableKind.VarVariable,
                id: this.id);

            // Declare this variable
            types.DeclareName(this.path, NamePayload.FromVariable(info));

            return new VarRefSyntaxC(
                info: info,
                assign: assign);
        }
    }

    public class VarRefSyntaxC : ISyntaxC {
        private readonly VariableInfo info;
        private readonly ISyntaxC assign;

        public ITrophyType ReturnType => this.info.Type;

        public VarRefSyntaxC(VariableInfo info, ISyntaxC assign) {
            this.info = info;
            this.assign = assign;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var assign = this.assign.GenerateCode(declWriter, statWriter);
            var typeName = declWriter.ConvertType(this.assign.ReturnType);

            var stat = CStatement.VariableDeclaration(
                   typeName,
                   "$" + this.info.Name + this.info.UniqueId,
                   assign);

            statWriter.WriteStatement(CStatement.Comment($"Definition of variable '{this.info.Name}'"));
            statWriter.WriteStatement(stat);
            statWriter.WriteStatement(CStatement.NewLine());

            return CExpression.AddressOf(CExpression.VariableLiteral("$" + this.info.Name + this.info.UniqueId));
        }
    }
}