using System.Globalization;

namespace JT_2_DT
{
    internal class UndirectedGraph
    {
        public HashSet<int> Leaves { get; private set; } = new HashSet<int>();

        private readonly List<HashSet<int>> _edges;
        
        public UndirectedGraph(int vertexCount)
        {
            _edges = new(vertexCount);
            for (int i = 0; i < vertexCount; i++)
            {
                _edges.Add(new HashSet<int>());
            }
        }

        // thread safe
        public void AddEdge(int v1, int v2)
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
        public HashSet<int> BanishNode(int node)
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

            return newLeaves;
        }

        // thread safe
        public void PurgeLeavesInRange(HashSet<int> range)
        {
            Stack<int> stack = new(range);
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

        public void ResolveToFullBinaryTree()
        {
            // we know leaves will not change during any time when this is called
            Queue<int> pendingNodes = new(Leaves);
            HashSet<int> processedNodes = new();

            while (pendingNodes.Any())
            {
                int nextLeaf = pendingNodes.Dequeue();
                int parent = _edges[nextLeaf].Except(processedNodes).FirstOrDefault();
                lock (_edges)
                {
                    var children = _edges[nextLeaf].Intersect(processedNodes);

                    if (children.Count() == 1)
                    {
                        foreach (int child in children)
                        {
                            _edges[nextLeaf].Remove(child);
                            _edges[child].Remove(nextLeaf);
                            AddEdge(child, parent);
                        }
                    }
                    else
                    {
                        while (children.Count() > 2)
                        {
                            bool first = true;

                            // add an intermediate node and connect it to nextleaf
                            int newIntermediate = _edges.Count;
                            _edges.Add(new() { nextLeaf });
                            _edges[nextLeaf].Add(newIntermediate);

                            // move each node except for the last one to the new node
                            foreach (int child in children)
                            {
                                if (!first)
                                {
                                    // remove from nextLeaf
                                    _edges[nextLeaf].Remove(child);
                                    _edges[child].Remove(nextLeaf);

                                    // add to newIntermediate
                                    AddEdge(child, newIntermediate);
                                }
                                else
                                {
                                    first = false;
                                }
                            }

                            children = _edges[newIntermediate].Intersect(processedNodes);
                        }
                    }

                    pendingNodes.Enqueue(nextLeaf);
                    if (_edges[parent].Except(processedNodes).Count() == 1)
                    {
                        pendingNodes.Enqueue(parent);
                    }
                }
            }
        }

        private bool LeafCheck(int node)
        {
            lock (Leaves)
            {
                if (_edges[node].Count == 1)
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

        public void ExtendNode(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _edges.Add(new());
            }
        }

        // thread safe
        public bool CheckConnection(int v1, int v2)
        {
            lock (_edges[v1])
            {
                return _edges[v1].Contains(v2);
            }
        }
    }
}
