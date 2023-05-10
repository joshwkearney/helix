using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Analysis.Lifetimes;
using System;

namespace Helix.Features.Aggregates {
    public class PutStructSyntax : ISyntaxTree {
        private readonly bool isTypeChecked;
        private readonly StructSignature sig;
        private readonly IReadOnlyList<string?> names;
        private readonly IReadOnlyList<ISyntaxTree> values;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure { get; }

        public PutStructSyntax(
            TokenLocation loc, 
            StructSignature sig,
            IReadOnlyList<string?> names,
            IReadOnlyList<ISyntaxTree> values, bool isTypeChecked = false) {

            this.Location = loc;
            this.sig = sig;
            this.names = names;
            this.values = values;
            this.isTypeChecked = isTypeChecked;

            this.IsPure = this.values.All(x => x.IsPure);
        }

        public ISyntaxTree CheckTypes(EvalFrame types) {
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
                    $"This initializer contains the duplicate member '{dups.First()}'");
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
                    $"The member '{undefinedFields.First()}' does not exist in the "
                        + $"struct type '{type}'");
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
                    $"The unspecified struct member '{requiredAbsentFields.First()}' does not have a default "
                        + "value and must be provided in the struct initializer");
            }

            var presentFields = names
                .Zip(this.values)
                .ToDictionary(x => x.First, x => x.Second);

            var allNames = sig.Members.Select(x => x.Name).ToArray();
            var allValues = new List<ISyntaxTree>();

            // Unify the arguments to the correct type
            foreach (var mem in sig.Members) {
                if (!presentFields.TryGetValue(mem.Name, out var value)) {
                    value = new VoidLiteral(this.Location);
                }

                value = value.CheckTypes(types).UnifyTo(mem.Type, types);
                allValues.Add(value);
            }

            var result = new PutStructSyntax(this.Location, this.sig, allNames, allValues, true);

            types.ReturnTypes[result] = type;
            return result;
        }

        public ISyntaxTree ToRValue(EvalFrame types) {
            if (!this.isTypeChecked) {
                throw TypeCheckingErrors.RValueRequired(this.Location);
            }

            return this;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            var bundleDict = new Dictionary<IdentifierPath, Lifetime>();

            // Add each member to the lifetime bundle
            for (int i = 0; i < this.names.Count; i++) {
                var name = this.names[i];
                var value = this.values[i];

                value.AnalyzeFlow(flow);

                // Go through each member of this field
                foreach (var (relPath, lifetime) in value.GetLifetimes(flow).Components) {
                    var memPath = new IdentifierPath(name).Append(relPath);

                    // Add this member to the lifetime dict
                    bundleDict[memPath] = lifetime;
                }
            }

            bundleDict[new IdentifierPath()] = Lifetime.None;
            this.SetLifetimes(new LifetimeBundle(bundleDict), flow);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            if (!this.isTypeChecked) {
                throw new InvalidOperationException();
            }

            var name = writer.GetVariableName();

            var varDecl = new CVariableDeclaration() {
                Type = writer.ConvertType(new NamedType(this.sig.Path)),
                Name = name
            };

            var mems = this.values
                .Select(x => x.GenerateCode(types, writer))
                .ToArray();

            if (!mems.Any()) {
                mems = new[] { new CIntLiteral(0) };
            }

            return new CCompoundExpression() {
                Arguments = mems,
                Type = writer.ConvertType(types.ReturnTypes[this])
            };
        }
    }
}