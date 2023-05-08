using Helix.Analysis;
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
        //public static IEnumerable<IdentifierPath> GetRemoteMemberPaths(HelixType type, EvalFrame types) {
        //    return GetMemberPaths(type, types)
        //        .Where(x => x.type.IsRemote(types))
        //        .Select(x => x.path);
        //}

        public static IEnumerable<(IdentifierPath path, HelixType type)> GetMemberPaths(HelixType type, ITypedFrame types) {
            return GetMemberPathsHelper(new IdentifierPath(), type, types);
        }

        private static IEnumerable<(IdentifierPath path, HelixType type)> GetMemberPathsHelper(
            IdentifierPath basePath, 
            HelixType type, 
            ITypedFrame types) {

            yield return (basePath, type);

            if (type is not NamedType named) {
                yield break;
            }

            if (!types.Structs.TryGetValue(named.Path, out var structSig)) {
                yield break;
            }

            foreach (var mem in structSig.Members) {
                var path = basePath.Append(mem.Name);

                foreach (var subs in GetMemberPathsHelper(path, mem.Type, types)) {
                    yield return subs;
                }
            }
        }
    }
}