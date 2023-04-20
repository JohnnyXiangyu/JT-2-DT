namespace JT_2_DT
{
    internal class UndirectedJtree
    {
        private readonly UndirectedGraph _tree;
        private readonly List<HashSet<int>> _clusterMapping;
        private int _nodeCount;

        public UndirectedJtree(int vertexCount)
        {
            _nodeCount = vertexCount;
            _tree = new(vertexCount);
            _clusterMapping = new List<HashSet<int>>(vertexCount);
            for (int i = 0; i < vertexCount; i++)
            {
                _clusterMapping.Add(new());
            }

            throw new NotImplementedException();
            // TODO: add edges one by one
            // TODO: add clusters one by one
        }

        public void MakeDtree(IEnumerable<IEnumerable<int>> families)
        {
            // extend current infrastructure
            HashSet<int> oldLeaves = new(_tree.Leaves);
            foreach (var fam in families)
            {
                _clusterMapping.Add(new(fam));
            }
            _tree.ExtendNode(families.Count());
            
            // insert the families into the tree
            var tasks = families.Select((fam, index) => Task.Run(() =>
            {
                int newNodeIndexHere = _nodeCount + index;
                InsertFamily(fam, newNodeIndexHere);
            }));
            Task.WaitAll(tasks.ToArray());

            // purge out useless leaves
            _tree.PurgeLeavesInRange(oldLeaves);
            
            // finalize insertion by updating node count
            _nodeCount += families.Count();

            // last step is to resolve the tree to ensure it's a full binary tree
            _tree.ResolveToFullBinaryTree();
        }

        private void InsertFamily(IEnumerable<int> family, int newIndex)
        {
            for (int i = 0; i < _nodeCount; i++)
            {
                if (_clusterMapping[i].IsSupersetOf(family))
                {
                    _tree.AddEdge(i, newIndex);
                }
            }

            throw new FamilyNotClusteredException();
        }

        public class FamilyNotClusteredException : Exception { }
    }
}
