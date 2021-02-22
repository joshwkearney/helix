using System.Collections.Immutable;
using System.Linq;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;

namespace Attempt20.Features.Variables {
    public class LetSyntaxA : ISyntaxA {
        private readonly string name;
        private readonly ISyntaxA assign;

        public TokenLocation Location { get; }

        public LetSyntaxA(TokenLocation loc, string name, ISyntaxA assign) {
            this.Location = loc;
            this.name = name;
            this.assign = assign;
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

            return new LetSyntaxB(
                loc: this.Location, 
                path: path, 
                id: names.GetNewVariableId(),
                region: names.CurrentRegion, 
                assign: assign);
        }
    }

    public class LetSyntaxB : ISyntaxB {
        private readonly IdentifierPath path;
        private readonly IdentifierPath region;
        private readonly int id;
        private readonly ISyntaxB assign;

        public TokenLocation Location { get; }

        public LetSyntaxB(TokenLocation loc, IdentifierPath path, int id, IdentifierPath region, ISyntaxB assign) {
            this.Location = loc;
            this.path = path;
            this.id = id;
            this.assign = assign;
            this.region = region;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var assign = this.assign.CheckTypes(types);

            var info = new VariableInfo(
                name: this.path.Segments.Last(),
                innerType: assign.ReturnType,
                kind: VariableDefinitionKind.Local,
                id: this.id,
                valueLifetimes: assign.Lifetimes,
                variableLifetimes: new[] { this.region }.ToImmutableHashSet());

            // Declare this variable
            types.DeclareVariable(this.path, info);

            return new LetSyntaxC(
                info: info,
                assign: assign,
                returnType: TrophyType.Void);
        }
    }

    public class LetSyntaxC : ISyntaxC {
        private readonly VariableInfo info;
        private readonly ISyntaxC assign;

        public TrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes => new IdentifierPath[0].ToImmutableHashSet();

        public LetSyntaxC(VariableInfo info, ISyntaxC assign, TrophyType returnType) {
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