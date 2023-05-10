using JT_2_DT.Utils;
using System.Collections.Concurrent;

namespace JT_2_DT
{
    internal class Dtree
    {
        public HashSet<int> Leaves { get; private set; } = new HashSet<int>();

        private List<HashSet<int>> _edges = new();
        private List<HashSet<int>> _clusterMapping = new();
        private readonly ConcurrentDictionary<int, int> _clauseMapping = new();
        private int _nodeCount;
        private int _rootByConvention;

        public Dtree(string filePath, IEnumerable<IEnumerable<int>> families)
        {
            LoadTreeDecompFile(filePath);
            MakeDtree(families);
        }

        private void LoadTreeDecompFile(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            if (lines == null)
            {
                throw new FileLoadException();
            }

            foreach (var line in lines)
            {
                switch (line[0])
                {
                    case 's':
                        {
                            var words = line.Split(' ');
                            int bagCount = int.Parse(words[2]);
                            // int width = int.Parse(words[3]) - 1;

                            _nodeCount = bagCount;
                            _edges = new(bagCount);
                            for (int i = 0; i < bagCount; i++)
                            {
                                _edges.Add(new HashSet<int>());
                            }
                            _clusterMapping = new List<HashSet<int>>(bagCount);
                            for (int i = 0; i < bagCount; i++)
                            {
                                _clusterMapping.Add(new());
                            }

                            break;
                        }
                    case 'b':
                        {
                            var words = line.Split(' ');
                            int clusterIndex = int.Parse(words[1]) - 1;

                            for (int i = 2; i < words.Length; i++)
                            {
                                _clusterMapping[clusterIndex].Add(int.Parse(words[i]) - 1);
                            }

                            break;
                        }
                    case 'c':
                        break;
                    default:
                        {
                            var words = line.Split(' ');
                            int b1 = int.Parse(words[0]) - 1;
                            int b2 = int.Parse(words[1]) - 1;
                            AddEdge(b1, b2);

                            break;
                        }
                }
            }

        }

        // thread safe
        private void AddEdge(int v1, int v2)
        {
            lock (_edges[v1])
            {
                _edges[v1].Add(v2);

            }

            lock (_edges[v2])
            {
                _edges[v2].Add(v1);
            }

            LeafCheck(v1);
            LeafCheck(v2);
        }

        // thread safe
        private HashSet<int> BanishNode(int node)
        {
            HashSet<int> targets;
            lock (_edges[node])
            {
                targets = new(_edges[node]);
            }

            HashSet<int> newLeaves = new();
            foreach (int v2 in targets)
            {
                lock (_edges[node])
                {
                    _edges[node].Remove(v2);
                }

                lock (_edges[v2])
                {
                    _edges[v2].Remove(node);
                }

                if (LeafCheck(v2))
                {
                    newLeaves.Add(v2);
                }
            }

            Leaves.Remove(node);

            return newLeaves;
        }

        // thread safe
        private void PurgeLeavesInRange(HashSet<int> range)
        {
            Stack<int> stack = new(range.Intersect(Leaves));
            while (stack.Any())
            {
                int nextLeaf = stack.Pop();

                HashSet<int> newLeaves = BanishNode(nextLeaf);
                foreach (int leaf in range.Intersect(newLeaves))
                {
                    stack.Push(leaf);
                }
            }
        }

        private void MakeDtree(IEnumerable<IEnumerable<int>> families)
        {
            // extend current infrastructure
            HashSet<int> oldLeaves = new(Leaves);

            foreach (var fam in families)
            {
                _clusterMapping.Add(new(fam));
            }
            ExtendNode(families.Count());

            // insert the families into the tree
            var tasks = families.Select((fam, index) => Task.Run(() =>
            {
                int newNodeIndexHere = _nodeCount + index;
                InsertFamily(fam, newNodeIndexHere);
                _clauseMapping[newNodeIndexHere] = index;
            }));
            Task.WaitAll(tasks.ToArray());

            // purge out useless leaves
            PurgeLeavesInRange(oldLeaves);

            // finalize insertion by updating node count
            _nodeCount += families.Count();

            // last step is to resolve the tree to ensure it's a full binary tree
            _rootByConvention = ResolveAsBinaryTree();
        }

        private void InsertFamily(IEnumerable<int> family, int newIndex)
        {
            for (int i = 0; i < _nodeCount; i++)
            {
                if (_clusterMapping[i].IsSupersetOf(family))
                {
                    AddEdge(i, newIndex);
                    return;
                }
            }
        }

        private int ResolveAsBinaryTree()
        {
            // we know leaves will not change during any time when this is called
            UniqueQueue<int> pendingNodes = new(Leaves);
            HashSet<int> processedNodes = new();

            int conventionalRoot = -1;

            while (pendingNodes.Any())
            {
                int currentLeaf = pendingNodes.SafeDequeue();
                bool isRoot = !_edges[currentLeaf].Except(processedNodes).Any();
                int parent;
                if (!isRoot)
                {
                    parent = _edges[currentLeaf].Except(processedNodes).FirstOrDefault();
                }
                else
                {
                    parent = -1;
                }

                lock (_edges)
                {
                    var children = _edges[currentLeaf].Intersect(processedNodes);

                    if (children.Count() == 1)
                    {
                        foreach (int child in children)
                        {
                            // detatch this node from the tree
                            _edges[currentLeaf].Remove(child);
                            _edges[child].Remove(currentLeaf);
                            _edges[parent].Remove(currentLeaf);
                            _edges[currentLeaf].Remove(parent);

                            if (!isRoot)
                            {
                                AddEdge(child, parent);
                            }
                        }
                    }
                    else
                    {
                        int target = currentLeaf;
                        while (children.Count() > 2)
                        {
                            int newIntermediate = ExtendNode(target, children);

                            children = _edges[newIntermediate].Intersect(processedNodes);
                            target = newIntermediate;
                        }
                    }

                    // register the node to processed nodes
                    processedNodes.Add(currentLeaf);

                    if (!isRoot)
                    {
                        if (_edges[parent].Except(processedNodes).Count() <= 1)
                        {
                            pendingNodes.SafeEnqueue(parent);
                        }
                    }
                    else
                    {
                        conventionalRoot = currentLeaf;
                    }
                }
            }

            return conventionalRoot;
        }

        private int ExtendNode(int target, IEnumerable<int> children)
        {
            bool first = true;

            // add an intermediate node and connect it to nextleaf
            int newIntermediate = DuplicateBag(target);

            // move each node except for the last one to the new node
            foreach (int child in children)
            {
                if (child == newIntermediate)
                    continue;

                if (first)
                {
                    first = false;
                }
                else
                {
                    // remove from nextLeaf
                    _edges[target].Remove(child);
                    _edges[child].Remove(target);

                    // add to newIntermediate
                    AddEdge(child, newIntermediate);
                }
            }

            return newIntermediate;
        }

        private int DuplicateBag(int target)
        {
            int newIntermediate = _edges.Count;
            _edges.Add(new() { target });
            _edges[target].Add(newIntermediate);
            _nodeCount++;
            _clusterMapping.Add(_clusterMapping[target]);
            return newIntermediate;
        }

        public Func<string>[] SerializeAsDtree()
        {
            // start with the conventional root, run a top-down bfs
            UniqueQueue<int> pendingNodes = new(new int[] { _rootByConvention });
            HashSet<int> processedNodes = new();
            Dictionary<int, int> graphNodeToSerializeNode = new();

            // initialize the result array statically
            Func<string>[] result = new Func<string>[2 * Leaves.Count];
            result[0] = () => $"dtree {2 * Leaves.Count - 1}";

            // partition the array into 2 segments
            int leafIndex = 1;
            int internalIndex = leafIndex + Leaves.Count;

            while (pendingNodes.Any())
            {
                int currentNode = pendingNodes.SafeDequeue();
                var children = _edges[currentNode].Except(processedNodes);

                int outputIndexUsed;

                if (children.Any())
                {
                    int c1 = children.ElementAt(0);
                    int c2 = children.ElementAt(1);

                    result[internalIndex] = () => 
                    {
                        return $"I {graphNodeToSerializeNode[c1]} {graphNodeToSerializeNode[c2]}"; 
                    };
                    outputIndexUsed = internalIndex;
                    internalIndex++;

                    foreach (var child in children)
                    {
                        pendingNodes.SafeEnqueue(child);
                    }
                }
                else
                {
                    result[leafIndex] = () => $"L {_clauseMapping[currentNode]}";
                    outputIndexUsed = leafIndex;
                    leafIndex++;
                }

                // register the node to processed nodes
                processedNodes.Add(currentNode);
                graphNodeToSerializeNode.Add(currentNode, outputIndexUsed - 1);
            }

            return result;
        }

        private bool LeafCheck(int node)
        {
            lock (Leaves)
            {
                if (_edges[node].Count <= 1)
                {
                    Leaves.Add(node);
                    return true;
                }
                else
                {
                    Leaves.Remove(node);
                    return false;
                }
            }
        }

        private void ExtendNode(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _edges.Add(new());
            }
        }
    }
}
