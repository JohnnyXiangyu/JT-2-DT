using JT_2_DT.Utils;
using System.Collections.Concurrent;

namespace JT_2_DT
{
    internal class Dtree
    {
        public HashSet<int> Leaves { get; private set; } = new HashSet<int>();

        private List<HashSet<int>> _edges = new();
        private List<HashSet<int>> _clusterMapping = new();
        private readonly ConcurrentDictionary<int, int> _nodeToClauseIndex = new();
        private int _nodeCount;
        private int _rootByConvention;

        public Dtree(string filePath, IEnumerable<IEnumerable<int>> families)
        {
            LoadTreeDecompFile(filePath);
            MakeDtree(families);
        }

        /// <summary>
        /// Deserialize a tree decomposition file, the output of a tree decomposition compiler.
        /// </summary>
        /// <param name="filePath">path to the file</param>
        /// <exception cref="FileLoadException"></exception>
        private void LoadTreeDecompFile(string filePath)
        {
            var lines = File.ReadAllLines(filePath) ?? throw new FileLoadException();

            foreach (var line in lines)
            {
                switch (line[0])
                {
                    case 's':
                        {
                            var words = line.Split(' ');
                            int bagCount = int.Parse(words[2]);

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
                                // variables start with 1
                                _clusterMapping[clusterIndex].Add(int.Parse(words[i])); 
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

        /// <summary>
        /// Add an bi-directional edge.
        /// </summary>
        /// <param name="v1">a node</param>
        /// <param name="v2">another node</param>
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

        /// <summary>
        /// Fully delete a node and from the graph.
        /// </summary>
        /// <param name="node">the node to delete</param>
        /// <returns>new leaves created by removing this node</returns>
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

        /// <summary>
        /// Delete all leaf nodes in a range.
        /// </summary>
        /// <param name="range">a set (hashset) of nodes</param>
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

        /// <summary>
        /// Convert the tree decomposition graph into a dtree, with the given families.
        /// </summary>
        /// <param name="families"></param>
        private void MakeDtree(IEnumerable<IEnumerable<int>> families)
        {
            HashSet<int> oldLeaves = new(Leaves);

            foreach (var fam in families)
            {
                _clusterMapping.Add(new(fam));
            }
            ExtendNode(families.Count());

            // insert the families into the tree
            var tasks = families.Select((fam, index) => Task.Run(() =>
            {
                // index := index of clause
                // fam := the family created from this clause (reduced to a hashset)
                int newNodeIndex = _nodeCount + index;
                InsertFamily(fam, newNodeIndex);
                _nodeToClauseIndex[newNodeIndex] = index;
            }));
            Task.WaitAll(tasks.ToArray());

            // purge out useless leaves
            PurgeLeavesInRange(oldLeaves);

            // finalize insertion by updating node count
            _nodeCount += families.Count();

            // last step is to resolve the tree to ensure it's a full binary tree
            _rootByConvention = ResolveAsBinaryTree();
        }

        /// <summary>
        /// Insert a new node that originates from a family from the source CNF.
        /// </summary>
        /// <param name="family">a set of variables</param>
        /// <param name="newIndex">the node to which this family is inserted</param>
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

        /// <summary>
        /// The rooting and reduction step combined into 1.
        /// </summary>
        /// <returns>a root by convention (tree decomposition is a undirected graph)</returns>
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

        /// <summary>
        /// "Extend" the target node to reduce its child-count to 2.
        /// This process creates an intermediate node.
        /// </summary>
        /// <param name="target">the original parent of all the provided children</param>
        /// <param name="children">children of this node</param>
        /// <returns>the created intermediate node's index</returns>
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

        /// <summary>
        /// Add a new jtree node that has the same bag as the target jtree node.
        /// </summary>
        /// <param name="target">a jtree node</param>
        /// <returns>the created node</returns>
        private int DuplicateBag(int target)
        {
            int newIntermediate = _edges.Count;
            _edges.Add(new() { target });
            _edges[target].Add(newIntermediate);
            _nodeCount++;
            _clusterMapping.Add(_clusterMapping[target]);
            return newIntermediate;
        }

        /// <summary>
        /// Convert internal data structures into a dtree file recognizable by C2D.
        /// It's designed with this signature to defer evaluation in an effort to avoid 
        /// inverted dependency between creation of serialized node and reference to serialized node.
        /// </summary>
        /// <returns>a sequence of lambdas that return each line of the desired output file</returns>
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
                    result[leafIndex] = () => $"L {_nodeToClauseIndex[currentNode]}";
                    outputIndexUsed = leafIndex;
                    leafIndex++;
                }

                // register the node to processed nodes
                processedNodes.Add(currentNode);
                graphNodeToSerializeNode.Add(currentNode, outputIndexUsed - 1);
            }

            return result;
        }

        /// <summary>
        /// Verify if the given node is a leaf or not.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Extend entries into the adjacency list.
        /// </summary>
        /// <param name="count"></param>
        private void ExtendNode(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _edges.Add(new());
            }
        }
    }
}
