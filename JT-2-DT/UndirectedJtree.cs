namespace JT_2_DT
{
    internal class UndirectedJtree
    {
        private readonly UndirectedGraph _tree;
        private readonly List<HashSet<int>> _clusterMapping = new();
        private int _nodeCount;

        public UndirectedJtree(string filePath)
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
                            _tree = new(bagCount);
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

                            for (int i = 2; i < words.Length; i ++)
                            {
                                _clusterMapping[clusterIndex].Add(int.Parse(words[i]));
                            }

                            break;
                        }
                    case 'c':
                        break;
                    default:
                        {
                            var words = line.Split(' ');
                            int v1 = int.Parse(words[0]) - 1;
                            int v2 = int.Parse(words[1]) - 1;
                            _tree!.AddEdge(v1, v2);

                            break;
                        }
                }
            }
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
