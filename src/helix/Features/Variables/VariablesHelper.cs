using Helix.Analysis;
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
        public static IEnumerable<IdentifierPath> GetMemberPaths(HelixType type, SyntaxFrame types) {
            return GetMemberPathsHelper(new IdentifierPath(), type, types);
        }

        private static IEnumerable<IdentifierPath> GetMemberPathsHelper(
            IdentifierPath basePath, 
            HelixType type, 
            SyntaxFrame types) {

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

                yield return path;

                foreach (var subs in GetMemberPathsHelper(path, mem.Type, types)) {
                    yield return subs;
                }
            }
        }
    }
}