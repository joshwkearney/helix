using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Parsing;
using Trophy.Generation.Syntax;
using Trophy.Generation;
using Trophy.Features.Primitives;

namespace Trophy.Features.Aggregates {
    public class NewAggregateSyntax : ISyntax {
        private readonly bool isTypeChecked;
        private readonly NamedType returnType;
        private readonly IReadOnlyList<string> names;
        private readonly IReadOnlyList<ISyntax> values;

        public TokenLocation Location { get; }

        public NewAggregateSyntax(TokenLocation loc, NamedType type,
            IReadOnlyList<string> names,
            IReadOnlyList<ISyntax> values, bool isTypeChecked = false) {

            this.Location = loc;
            this.returnType = type;
            this.names = names;
            this.values = values;
            this.isTypeChecked = isTypeChecked;
        }

        public Option<TrophyType> TryInterpret(INamesRecorder names) => Option.None;

        public ISyntax CheckTypes(ITypesRecorder types) {
            var sig = types.GetAggregate(this.returnType.Path);

            // Skip all argument checking if there aren't any arguments
            //if (!this.names.Any()) {
            //    var result2 = new NewAggregateSyntax(
            //        this.Location,
            //        this.returnType,
            //        Array.Empty<string>(),
            //        Array.Empty<ISyntax>(),
            //        true);

            //    types.SetReturnType(result2, this.returnType);
            //    return result2;
            //}

            if (sig.Kind == AggregateKind.Struct) {
                return this.CheckStructTypes(sig, types);
            }
            else if (sig.Kind == AggregateKind.Union) {
                return this.CheckUnionTypes(sig, types);
            }
            else {
                throw new InvalidOperationException();
            }
        }

        private ISyntax CheckUnionTypes(AggregateSignature sig, ITypesRecorder types) {
            if (this.names.Count > 1) {
                throw new TypeCheckingException(
                    this.Location, 
                    "Bad Union Initialization",
                    "Unions cannot be initialized with more than one member.");
            }

            return this.CheckStructTypes(sig, types);
        }


        private ISyntax CheckStructTypes(AggregateSignature sig, ITypesRecorder types) {
            var type = new NamedType(sig.Path);

            var dups = this.names
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();

            // Make sure there are no duplicate names
            if (dups.Any()) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, dups.First());
            }

            var undefinedFields = this.names
                .Select(x => x)
                .Except(sig.Members.Select(x => x.MemberName))
                .ToArray();

            // Make sure that all members are defined in the struct
            if (undefinedFields.Any()) {
                throw TypeCheckingErrors.NewObjectHasExtraneousFields(this.Location, type, undefinedFields);
            }

            var absentFields = sig.Members
                .Select(x => x.MemberName)
                .Except(this.names)
                .Select(x => sig.Members.First(y => x == y.MemberName))
                .ToArray();

            var requiredAbsentFields = absentFields
                .Where(x => !x.MemberType.HasDefaultValue(types))
                .ToArray();

            // Make sure that all the missing members have a default value
            if (requiredAbsentFields.Any()) {
                throw TypeCheckingErrors.NewObjectMissingFields(
                    this.Location,
                    type,
                    requiredAbsentFields.Select(x => x.MemberName));
            }

            var presentFields = this.names
                .Zip(this.values)
                .ToDictionary(x => x.First, x => x.Second);

            var allNames = sig.Members.Select(x => x.MemberName).ToArray();
            var allValues = new List<ISyntax>();

            // Unify the arguments to the correct type
            foreach (var mem in sig.Members) {
                if (!presentFields.TryGetValue(mem.MemberName, out var value)) {
                    value = new VoidLiteral(this.Location);
                }

                value = value.CheckTypes(types).UnifyTo(mem.MemberType, types);
                allValues.Add(value);
            }

            var result = new NewAggregateSyntax(this.Location, type, allNames, allValues, true);
            types.SetReturnType(result, type);

            return result;
        }

        public ISyntax ToRValue(ITypesRecorder types) {
            if (!this.isTypeChecked) {
                throw TypeCheckingErrors.RValueRequired(this.Location);
            }

            return this;
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            if (!this.isTypeChecked) {
                throw new InvalidOperationException();
            }

            var name = writer.GetVariableName();

            var varDecl = new CVariableDeclaration() {
                Type = writer.ConvertType(this.returnType),
                Name = name
            };

            var mems = this.values
                .Select(x => x.GenerateCode(writer))
                .ToArray();

            if (!mems.Any()) {
                mems = new[] { new CIntLiteral(0) };
            }

            return new CCompoundExpression() {
                Arguments = mems
            };
        }
    }
}