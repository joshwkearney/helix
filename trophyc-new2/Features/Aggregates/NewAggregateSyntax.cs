using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Parsing;
using Trophy.Generation.Syntax;
using Trophy.Generation;
using Trophy.Features.Primitives;

namespace Trophy.Features.Aggregates {
    public class NewAggregateSyntax : ISyntax {
        private readonly bool isTypeChecked;
        private readonly AggregateSignature sig;
        private readonly IReadOnlyList<string?> names;
        private readonly IReadOnlyList<ISyntax> values;

        public TokenLocation Location { get; }

        public NewAggregateSyntax(TokenLocation loc, 
            AggregateSignature sig,
            IReadOnlyList<string?> names,
            IReadOnlyList<ISyntax> values, bool isTypeChecked = false) {

            this.Location = loc;
            this.sig = sig;
            this.names = names;
            this.values = values;
            this.isTypeChecked = isTypeChecked;
        }

        public ISyntax CheckTypes(ITypesRecorder types) {
            var names = new string[this.names.Count];
            int missingCounter = 0;

            // Fill in missing names
            for (int i = 0; i < names.Length; i++) {
                // If this name is defined then set it and move on
                if (this.names[i] != null) {
                    names[i] = this.names[i]!;

                    var index = this.sig.Members
                        .Select((x, i) => new { Index = i, Value = x.Name })
                        .Where(x => x.Value == this.names[i])
                        .Select(x => x.Index)
                        .First();

                    missingCounter = index + 1;
                    continue;
                }

                // Make sure we don't have too many arguments
                if (missingCounter >= this.sig.Members.Count) {
                    throw new TypeCheckingException(
                        this.Location,
                        "Invalid Initialization",
                        "This initializer has provided too many "
                            + $"arguments for the type '{new NamedType(this.sig.Path)}'");
                }

                names[i] = sig.Members[missingCounter++].Name;
            }

            if (sig.Kind == AggregateKind.Struct) {
                return this.CheckStructTypes(names!, types);
            }
            else if (sig.Kind == AggregateKind.Union) {
                return this.CheckUnionTypes(names!, types);
            }
            else {
                throw new InvalidOperationException();
            }
        }

        private ISyntax CheckUnionTypes(IReadOnlyList<string> names, ITypesRecorder types) {
            if (names.Count > 1) {
                throw new TypeCheckingException(
                    this.Location, 
                    "Invalid Union Initialization",
                    "Unions cannot be initialized with more than one member.");
            }

            ISyntax result;

            // If there aren't any assigned members then assigned the first one
            if (names.Count == 0) {
                var values = new[] { 
                    new VoidLiteral(this.Location)
                        .CheckTypes(types)
                        .UnifyTo(sig.Members[0].Type, types) 
                };

                result = new NewAggregateSyntax(
                    this.Location,
                    this.sig,
                    new[] { sig.Members[0].Name },
                    values,
                    true);
            }
            else {
                // Make sure the member is defined on this union
                if (!sig.Members.Where(x => x.Name == names[0]).FirstOrNone().TryGetValue(out var mem)) {
                    throw new TypeCheckingException(
                        this.Location,
                        "Invalid Union Initialization",
                        $"The member '{ names[0] }' does not exist in the " 
                            + "union type '{new NamedType(this.sig.Path)}'");
                }

                // Make sure that all the other union members have default values
                var noDefault = sig.Members
                    .Where(x => x.Name != names[0])
                    .Where(x => !x.Type.HasDefaultValue(types))
                    .Select(x => x.Name)
                    .FirstOrNone();

                if (noDefault.HasValue) {
                    throw new TypeCheckingException(
                        this.Location,
                        "Invalid Union Initialization",
                        $"The unspecified union member '{noDefault.GetValue()}' does not have a " 
                            + "default value and must be provided in the union initializer");
                }

                var values = new[] { 
                    this.values[0]
                        .CheckTypes(types)
                        .UnifyTo(mem.Type, types) 
                };

                result = new NewAggregateSyntax(
                    this.Location,
                    this.sig,
                    new[] { names[0] },
                    values,
                    true);
            }

            types.SetReturnType(result, new NamedType(sig.Path));
            return result;
        }


        private ISyntax CheckStructTypes(IReadOnlyList<string> names, ITypesRecorder types) {
            var type = new NamedType(sig.Path);

            var dups = names
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();

            // Make sure there are no duplicate names
            if (dups.Any()) {
                throw new TypeCheckingException(
                    this.Location,
                    "Invalid Struct Initialization", 
                    $"This initializer contains the duplicate member '{ dups.First() }'");
            }

            var undefinedFields = names
                .Select(x => x)
                .Except(sig.Members.Select(x => x.Name))
                .ToArray();

            // Make sure that all members are defined in the struct
            if (undefinedFields.Any()) {
                throw new TypeCheckingException(
                    this.Location,
                    "Invalid Struct Initialization",
                    $"The member '{ undefinedFields.First() }' does not exist in the "
                        + $"struct type '{ type }'");
            }

            var absentFields = sig.Members
                .Select(x => x.Name)
                .Except(names)
                .Select(x => sig.Members.First(y => x == y.Name))
                .ToArray();

            var requiredAbsentFields = absentFields
                .Where(x => !x.Type.HasDefaultValue(types))
                .Select(x => x.Name)
                .ToArray();

            // Make sure that all the missing members have a default value
            if (requiredAbsentFields.Any()) {
                throw new TypeCheckingException(
                    this.Location,
                    "Invalid Struct Initialization",
                    $"The unspecified struct member '{ requiredAbsentFields.First() }' does not have a default "
                        + "value and must be provided in the struct initializer");
            }

            var presentFields = names
                .Zip(this.values)
                .ToDictionary(x => x.First, x => x.Second);

            var allNames = sig.Members.Select(x => x.Name).ToArray();
            var allValues = new List<ISyntax>();

            // Unify the arguments to the correct type
            foreach (var mem in sig.Members) {
                if (!presentFields.TryGetValue(mem.Name, out var value)) {
                    value = new VoidLiteral(this.Location);
                }

                value = value.CheckTypes(types).UnifyTo(mem.Type, types);
                allValues.Add(value);
            }

            var result = new NewAggregateSyntax(this.Location, this.sig, allNames, allValues, true);
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
                Type = writer.ConvertType(new NamedType(this.sig.Path)),
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