using System.Diagnostics;

namespace Helix.IRGeneration;

public class IRSimplifier {
    public Dictionary<string, Block> Simplify(Dictionary<string, Block> blocks) {
        var predecessors = this.ComputePredecessors(blocks);

        this.RemoveDeadBlocks(blocks, predecessors);
        this.RemoveEmptyBlocks(blocks);
        this.MergeLinearBlocks(blocks, predecessors);
        
        return blocks;
    }

    private void RemoveDeadBlocks(
        Dictionary<string, Block> blocks, 
        Dictionary<string, IReadOnlyList<string>> predecessors) {
        
        // We're using the predecessor to iterate to avoid mutation during iteration
        foreach (var name in predecessors.Keys) {
            var block = blocks[name];

            if (block.Index != 0 && predecessors[name].Count == 0) {
                blocks.Remove(name);
            }
        }
    }

    private void RemoveEmptyBlocks(Dictionary<string, Block> blocks) {
        var renames = new Dictionary<string, string>();

        // Find all the blocks that we need to rename
        foreach (var block in blocks.Values) {
            if (block.IsEmpty && block.Successors.Length == 1) {
                renames[block.Name] = block.Successors[0];
            }
        }

        // Remove the blocks we're going to rename
        foreach (var name in renames.Keys) {
            blocks.Remove(name);
        }
        
        // Rename all the jumps in our blocks to line up with the new names
        foreach (var block in blocks.Values) {
            Debug.Assert(block.Terminal != null);
            
            block.Terminate(block.Terminal.RenameBlocks(renames));
        }
    }

    private void MergeLinearBlocks(
        Dictionary<string, Block> blocks,
        Dictionary<string, IReadOnlyList<string>> predecessors) {
        
        var result = new Dictionary<string, string>();

        foreach (var first in blocks.Values) {
            var successors = first.Successors;
            if (successors.Length != 1) {
                continue;
            }

            var secondName = successors[0];
            if (predecessors[secondName].Count != 1) {
                continue;
            }

            // This block always goes to the second block, and the second block
            // always comes from the first. This means we can merge them
            this.MergeHelper(first, blocks[secondName], blocks);
        }
    }

    private void MergeHelper(Block first, Block second, Dictionary<string, Block> blocks) {
        Debug.Assert(second.Terminal != null);
        
        // Be sure to skip the jump at the end
        foreach (var op in second.Instructions.SkipLast(1)) {
            first.Add(op);
        }
        
        first.Terminate(second.Terminal);
        blocks.Remove(second.Name);
    }
    
    private Dictionary<string, IReadOnlyList<string>> ComputePredecessors(Dictionary<string, Block> blocks) {
        var result = new Dictionary<string, List<string>>();

        // Fill out all the predecessor lists for simplicity
        foreach (var name in blocks.Keys) {
            result[name] = [];
        }
        
        // Reverse our blocks graph
        foreach (var block in blocks.Values) {
            foreach (var successor in block.Successors) {
                result[successor].Add(block.Name);
            }
        }

        return result.ToDictionary(x => x.Key, x => (IReadOnlyList<string>)x.Value);
    }
}