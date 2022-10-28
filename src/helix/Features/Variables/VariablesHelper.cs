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
        public static IEnumerable<VariableSignature> GetSubSignatures(
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
                var memSig = new VariableSignature(path, mem.Type, mem.IsWritable, 0, false);

                yield return memSig;

                foreach (var subs in GetSubSignatures(path, mem.Type, types)) {
                    yield return subs;
                }
            }
        }
    }
}