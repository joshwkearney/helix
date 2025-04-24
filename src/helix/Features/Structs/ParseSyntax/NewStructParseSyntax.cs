using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Structs;

public class NewStructParseSyntax : IParseSyntax {
    public required TokenLocation Location { get; init; }
        
    public required StructType Signature { get; init; }

    public IReadOnlyList<string> Names { get; init; } = [];

    public IReadOnlyList<IParseSyntax> Values { get; init; } = [];
        
    public bool IsPure => this.Values.All(x => x.IsPure);

    public ISyntax CheckTypes(TypeFrame types) {
        var names = new string[this.Names.Count];
        int missingCounter = 0;

        // Fill in missing names
        for (int i = 0; i < names.Length; i++) {
            // If this name is defined then set it and move on
            if (this.Names[i] != null) {
                names[i] = this.Names[i]!;

                var index = this.Signature.Members
                    .Select((x, i) => new { Index = i, Value = x.Name })
                    .Where(x => x.Value == this.Names[i])
                    .Select(x => x.Index)
                    .First();

                missingCounter = index + 1;
                continue;
            }

            // Make sure we don't have too many arguments
            if (missingCounter >= this.Signature.Members.Count) {
                throw new TypeException(
                    this.Location,
                    "Invalid Initialization",
                    "This initializer has provided too many "
                  + $"arguments for the type '{this.Signature}'");
            }

            names[i] = this.Signature.Members[missingCounter++].Name;
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
            .Except(this.Signature.Members.Select(x => x.Name))
            .ToArray();

        // Make sure that all members are defined in the struct
        if (undefinedFields.Any()) {
            throw new TypeException(
                this.Location,
                "Invalid Struct Initialization",
                $"The member '{undefinedFields.First()}' does not exist in the "
              + $"struct type '{this.Signature}'");
        }

        var absentFields = this.Signature.Members
            .Select(x => x.Name)
            .Except(names)
            .Select(x => this.Signature.Members.First(y => x == y.Name))
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
            .Zip(this.Values)
            .ToDictionary(x => x.First, x => x.Second);

        var allNames = this.Signature.Members.Select(x => x.Name).ToArray();
        var allValues = new List<ISyntax>();

        // Unify the arguments to the correct type
        foreach (var mem in this.Signature.Members) {
            if (!presentFields.TryGetValue(mem.Name, out var value)) {
                value = new VoidLiteral {
                    Location = this.Location
                };
            }

            var checkedValue = value.CheckTypes(types).UnifyTo(mem.Type, types);
                
            allValues.Add(checkedValue);
        }

        var result = new NewStructSyntax {
            Location = this.Location,
            Signature = this.Signature,
            Names = allNames,
            Values = allValues
        };

        return result;
    }
}