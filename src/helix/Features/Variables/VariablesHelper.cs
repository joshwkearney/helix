﻿using Helix.Analysis;
using Helix.Analysis.Lifetimes;
using Helix.Analysis.Types;
using Helix.Features.Aggregates;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Features.Variables {
    public static class VariablesHelper {
        public static IEnumerable<(IdentifierPath path, HelixType type)> GetMemberPaths(HelixType type, SyntaxFrame types) {
            return GetMemberPathsHelper(new IdentifierPath(), type, types);
        }

        public static ILifetimeBundle GetVariableLifetimes(IdentifierPath varPath, HelixType type, SyntaxFrame types) {
            var lifetimes = new Dictionary<IdentifierPath, IReadOnlyList<Lifetime>>();

            // Go through all this variable's members and set the lifetime bundle correctly
            foreach (var (compPath, _) in GetMemberPaths(type, types)) {
                var memPath = varPath.Append(compPath);

                lifetimes[compPath] = new[] { types.Variables[memPath].Lifetime };
            }

            return new StructLifetimeBundle(lifetimes);
        }

        private static IEnumerable<(IdentifierPath path, HelixType type)> GetMemberPathsHelper(
            IdentifierPath basePath, 
            HelixType type, 
            SyntaxFrame types) {

            yield return (basePath, type);

            if (type is not NamedType named) {
                yield break;
            }

            if (!types.Aggregates.TryGetValue(named.Path, out var agSig)) {
                yield break;
            }

            if (agSig.Kind != AggregateKind.Struct) {
                yield break;
            }

            foreach (var mem in agSig.Members) {
                var path = basePath.Append(mem.Name);

                foreach (var subs in GetMemberPathsHelper(path, mem.Type, types)) {
                    yield return subs;
                }
            }
        }
    }
}