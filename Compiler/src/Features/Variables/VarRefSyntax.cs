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

        public ISyntaxB CheckNames(INameRecorder names) {
            var assign = this.assign.CheckNames(names);
            var path = names.CurrentScope.Append(this.name);

            // Make sure we're not shadowing another variable
            if (names.TryFindName(this.name, out _, out _)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.name);
            }

            // Declare this variable
            names.DeclareLocalName(path, NameTarget.Variable);

            return new VarRefSyntaxB(
                loc: this.Location, 
                path: path, 
                id: names.GetNewVariableId(),
                region: names.CurrentRegion, 
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

        public VarRefSyntaxB(TokenLocation loc, IdentifierPath path, int id, IdentifierPath region, ISyntaxB assign, bool isreadonly) {
            this.Location = loc;
            this.path = path;
            this.id = id;
            this.assign = assign;
            this.region = region;
            this.isreadonly = isreadonly;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var assign = this.assign.CheckTypes(types);

            // If this is a var declaration and the type is a dependent type (singular int type, 
            // singular function type, or fixed array type, convert it to the more abstract form
            // so the type system doesn't get in the way
            if (!this.isreadonly) {
                if (assign.ReturnType.AsFixedArrayType().TryGetValue(out var fixedArrayType)) {
                    assign = types.TryUnifyTo(assign, new ArrayType(fixedArrayType.ElementType, fixedArrayType.IsReadOnly)).GetValue();
                }
            }

            var info = new VariableInfo(
                name: this.path.Segments.Last(),
                innerType: assign.ReturnType,
                kind: this.isreadonly ? VariableDefinitionKind.LocalRef : VariableDefinitionKind.LocalVar,
                id: this.id,
                valueLifetimes: assign.Lifetimes,
                variableLifetimes: new[] { this.region }.ToImmutableHashSet());

            // Declare this variable
            types.DeclareVariable(this.path, info);

            return new VarRefSyntaxC(
                info: info,
                assign: assign,
                returnType: TrophyType.Void);
        }
    }

    public class VarRefSyntaxC : ISyntaxC {
        private readonly VariableInfo info;
        private readonly ISyntaxC assign;

        public TrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => new IdentifierPath[0].ToImmutableHashSet();

        public VarRefSyntaxC(VariableInfo info, ISyntaxC assign, TrophyType returnType) {
            this.info = info;
            this.assign = assign;
            this.ReturnType = returnType;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var assign = this.assign.GenerateCode(declWriter, statWriter);
            var typeName = declWriter.ConvertType(this.assign.ReturnType);

            var stat = CStatement.VariableDeclaration(
                   typeName,
                   this.info.Name + this.info.UniqueId,
                   assign);

            statWriter.WriteStatement(stat);
            statWriter.WriteStatement(CStatement.NewLine());

            return CExpression.IntLiteral(0);
        }
    }
}