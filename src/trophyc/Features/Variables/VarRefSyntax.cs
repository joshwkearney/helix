using System.Collections.Immutable;
using System.Linq;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation.CSyntax;
using Trophy.Parsing;

namespace Trophy.Features.Variables {
    public class VarRefSyntaxA : ISyntaxA {
        private readonly string name;
        private readonly ISyntaxA assign;
        private readonly bool isReadonly;
        private readonly bool isHeapAllocated;

        public TokenLocation Location { get; }

        public VarRefSyntaxA(TokenLocation loc, string name, ISyntaxA assign, bool isreadonly, bool isHeapAllocated) {
            this.Location = loc;
            this.name = name;
            this.assign = assign;
            this.isReadonly = isreadonly;
            this.isHeapAllocated = isHeapAllocated;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            var assign = this.assign.CheckNames(names);
            var path = names.Context.Scope.Append(this.name);
            var region = this.isHeapAllocated 
                ? RegionsHelper.GetClosestHeap(names.Context.Region) 
                : RegionsHelper.GetClosestStack(names.Context.Region);

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
                region: region, 
                assign: assign,
                this.isReadonly);
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
                source: RegionsHelper.IsStack(this.region) ? VariableSource.Local : VariableSource.Parameter,
                kind: this.isreadonly ? VariableKind.RefVariable : VariableKind.VarVariable,
                id: this.id);

            // Declare this variable
            types.DeclareName(this.path, NamePayload.FromVariable(info));

            return new VarRefSyntaxC(
                info: info,
                assign: assign,
                this.region);
        }
    }

    public class VarRefSyntaxC : ISyntaxC {
        private readonly VariableInfo info;
        private readonly ISyntaxC assign;
        private readonly IdentifierPath regionName;

        public ITrophyType ReturnType => this.info.Type;

        public VarRefSyntaxC(VariableInfo info, ISyntaxC assign, IdentifierPath regionName) {
            this.info = info;
            this.assign = assign;
            this.regionName = regionName;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var assign = this.assign.GenerateCode(declWriter, statWriter);
            var varType = declWriter.ConvertType(this.assign.ReturnType);
            var name = "$" + this.info.Name + this.info.UniqueId;

            statWriter.WriteStatement(CStatement.Comment($"Definition of variable '{this.info.Name}'"));

            if (this.info.Source == VariableSource.Local) {
                var stat = CStatement.VariableDeclaration(
                    varType,
                    name,
                    assign);

                statWriter.WriteStatement(stat);
            }
            else {
                var ptrType = CType.Pointer(varType);

                var alloc = CExpression.Invoke(CExpression.VariableLiteral("region_alloc"), new[] {
                    CExpression.VariableLiteral(this.regionName.Segments.Last()),
                    CExpression.Sizeof(varType)
                });

                var stat1 = CStatement.VariableDeclaration(
                    ptrType,
                    name,
                    alloc);

                var stat2 = CStatement.Assignment(
                    CExpression.Dereference(CExpression.VariableLiteral(name)), 
                    assign);

                statWriter.WriteStatement(stat1);
                statWriter.WriteStatement(stat2);
            }

            statWriter.WriteStatement(CStatement.NewLine());

            return CExpression.AddressOf(CExpression.VariableLiteral("$" + this.info.Name + this.info.UniqueId));
        }
    }
}