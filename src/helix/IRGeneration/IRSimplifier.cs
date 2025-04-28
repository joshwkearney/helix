using System.Diagnostics;

namespace Helix.IRGeneration;

public class IRSimplifier {
    public Dictionary<string, Block> Simplify(Dictionary<string, Block> blocks) {
        var predecessors = this.ComputePredecessors(blocks);

        this.RemoveDeadBlocks(blocks, predecessors);
        this.RemoveEmptyBlocks(blocks, predecessors);
        this.MergeLinearBlocks(blocks, predecessors);
        
        return blocks;
    }

    private void RemoveDeadBlocks(
        Dictionary<string, Block> blocks, 
        Dictionary<string, List<string>> predecessors) {

        var toVisit = new Queue<string>(blocks.Keys);
        
        while (toVisit.Count > 0) {
            var name = toVisit.Dequeue();
            
            // Since we're removing as we go, this block might not exist anymore
            // Also don't remove the first block
            if (!blocks.TryGetValue(name, out var block) || block.Index == 0) {
                continue;
            }
            
            // This block has predecessors, so don't remove it
            if (predecessors[name].Count > 0) {
                continue;
            }
            
            // Remove the block and check our successors again so we can remove them
            // too
            this.RemoveBlock(name, blocks, predecessors);
            toVisit.EnqueueRange(blocks[name].Successors);
        }
    }

    private void RemoveEmptyBlocks(Dictionary<string, Block> blocks, Dictionary<string, List<string>> predecessors) {
        var renames = new Dictionary<string, string>();

        // Find all the blocks that we need to rename
        foreach (var block in blocks.Values) {
            // If this block has instructions or is a branch block, don't remove it
            if (!block.IsEmpty || block.Successors.Length == 0) {
                continue;
            }
            
            // Rename all instances of this block name to the name of our successor. 
            renames[block.Name] = block.Successors[0];
            
            // Now here's the trick, if we're renaming another block to this block name, we
            // have to update that rename as well
            foreach (var (oldName, _) in renames.Where(x => x.Value == block.Name).ToArray()) {
                renames[oldName] = block.Successors[0];
            }
        }
        
        // Rename all the jumps in our blocks to line up with the new names
        foreach (var block in blocks.Values) {
            Debug.Assert(block.Terminal != null);
            
            block.Terminate(block.Terminal.RenameBlocks(renames));
        }
        
        // Remove the blocks we're going to rename
        foreach (var name in renames.Keys) {
            this.RemoveBlock(name, blocks, predecessors);
        }
    }

    private void MergeLinearBlocks(
        Dictionary<string, Block> blocks,
        Dictionary<string, List<string>> predecessors) {
        
        var toVisit = new Queue<string>(blocks.Keys);

        while (toVisit.Count > 0) {
            var firstName = toVisit.Dequeue();
            if (!blocks.TryGetValue(firstName, out var first)) {
                continue;
            }
            
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
            this.MergeHelper(first, blocks[secondName], blocks, predecessors);
            
            // We might also be able to merge this block with another one again,
            // so let's check on it again
            toVisit.Enqueue(firstName);
        }
    }

    private void MergeHelper(
        Block first, 
        Block second, 
        Dictionary<string, Block> blocks,
        Dictionary<string, List<string>> predecessors) {
        
        Debug.Assert(second.Terminal != null);
        
        // Be sure to skip the jump at the end
        foreach (var op in second.Instructions.SkipLast(1)) {
            first.Add(op);
        }
        
        // Make sure the first block ends with the second one's jump
        first.Terminate(second.Terminal);
        
        // The second block's successors must have our first block as a prececessor now
        foreach (var item in second.Successors) {
            predecessors[item].Add(first.Name);
        }
        
        // Remove the second block from our graph
        this.RemoveBlock(second.Name, blocks, predecessors);
    }

    private void RemoveBlock(
        string name, 
        Dictionary<string, Block> blocks,
        Dictionary<string, List<string>> predecessors) {
        
        // Update the predecessor dictionary first to remove this block as a predecessor
        // of its successors. This will allow us to remove them as well
        foreach (var successor in blocks[name].Successors) {
            predecessors[successor].Remove(name);
        }
            
        // Now remove our block from the main dictionary
        blocks.Remove(name);
        predecessors[name].Clear();
    }
    
    private Dictionary<string, List<string>> ComputePredecessors(Dictionary<string, Block> blocks) {
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

        return result.ToDictionary(x => x.Key, x => x.Value);
    }
}