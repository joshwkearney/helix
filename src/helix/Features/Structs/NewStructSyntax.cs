using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;

namespace Helix.Features.Structs {
    public class NewStructSyntax : ISyntaxTree {
        private static int tempCounter = 0;

        private readonly bool isTypeChecked;
        private readonly StructType sig;
        private readonly HelixType structType;
        private readonly IReadOnlyList<string> names;
        private readonly IReadOnlyList<ISyntaxTree> values;
        private readonly IdentifierPath path;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure { get; }

        public NewStructSyntax(TokenLocation loc, HelixType structType, StructType sig,
                               IReadOnlyList<string> names, IReadOnlyList<ISyntaxTree> values, 
                               IdentifierPath scope, bool isTypeChecked = false) {
            this.Location = loc;
            this.sig = sig;
            this.structType = structType;
            this.names = names;
            this.values = values;
            this.isTypeChecked = isTypeChecked;
            this.path = scope.Append("$struct" + tempCounter++);

            this.IsPure = this.values.All(x => x.IsPure);
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            if (!this.isTypeChecked) {
                throw TypeException.RValueRequired(this.Location);
            }

            return this;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
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
                    throw new TypeException(
                        this.Location,
                        "Invalid Initialization",
                        "This initializer has provided too many "
                            + $"arguments for the type '{this.structType}'");
                }

                names[i] = this.sig.Members[missingCounter++].Name;
            }

            var dups = names
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();

            // Make sure there are no duplicate names
            if (dups.Any()) {
                throw new TypeException(
                    this.Location,
                    "Invalid Struct Initialization",
                    $"This initializer contains the duplicate member '{dups.First()}'");
            }

            var undefinedFields = names
                .Select(x => x)
                .Except(this.sig.Members.Select(x => x.Name))
                .ToArray();

            // Make sure that all members are defined in the struct
            if (undefinedFields.Any()) {
                throw new TypeException(
                    this.Location,
                    "Invalid Struct Initialization",
                    $"The member '{undefinedFields.First()}' does not exist in the "
                        + $"struct type '{this.structType}'");
            }

            var absentFields = this.sig.Members
                .Select(x => x.Name)
                .Except(names)
                .Select(x => this.sig.Members.First(y => x == y.Name))
                .ToArray();

            var requiredAbsentFields = absentFields
                .Where(x => !x.Type.HasDefaultValue(types))
                .Select(x => x.Name)
                .ToArray();

            // Make sure that all the missing members have a default value
            if (requiredAbsentFields.Any()) {
                throw new TypeException(
                    this.Location,
                    "Invalid Struct Initialization",
                    $"The unspecified struct member '{requiredAbsentFields.First()}' does not have a default "
                        + "value and must be provided in the struct initializer");
            }

            var presentFields = names
                .Zip(this.values)
                .ToDictionary(x => x.First, x => x.Second);

            var allNames = this.sig.Members.Select(x => x.Name).ToArray();
            var allValues = new List<ISyntaxTree>();

            // Unify the arguments to the correct type
            foreach (var mem in this.sig.Members) {
                if (!presentFields.TryGetValue(mem.Name, out var value)) {
                    value = new VoidLiteral(this.Location);
                }

                value = value.CheckTypes(types).UnifyTo(mem.Type, types);
                allValues.Add(value);
            }

            var result = new NewStructSyntax(
                this.Location,
                this.structType,
                this.sig, 
                allNames, 
                allValues,
                this.path,
                true);
            
            new SyntaxTagBuilder(types)
                .WithChildren(allValues)
                .WithReturnType(this.structType)
                .BuildFor(result);

            return result;
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            if (!this.isTypeChecked) {
                throw new InvalidOperationException();
            }

            var memValues = this.values
                .Select(x => x.GenerateCode(types, writer))
                .ToArray();

            if (!memValues.Any()) {
                memValues = new[] { new CIntLiteral(0) };
            }

            return new CCompoundExpression() {
                Type = writer.ConvertType(this.GetReturnType(types), types),
                MemberNames = this.names,
                Arguments = memValues,
            };
        }
    }
}