using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JT_2_DT
{
    internal class UndirectedJtree
    {
        private UndirectedGraph _tree;
        private HashSet<int>[] _clusterMapping;
        private int _nodeCount;

        public UndirectedJtree(int vertexCount)
        {
            _nodeCount = vertexCount;
            _tree = new(vertexCount);
            _clusterMapping = new HashSet<int>[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                _clusterMapping[i] = new();
            }

            throw new NotImplementedException();
            // TODO: add edges one by one
            // TODO: add clusters one by one
        }

        public void MakeDtree(IEnumerable<IEnumerable<int>> families)
        {
            // naive parallelism
            List<Task> tasks = new();
            int newNodeIndex = _nodeCount;
            foreach (var fam in families)
            {
                int newNodeIndexHere = newNodeIndex;
                tasks.Add(Task.Run(() => InsertFamily(fam, newNodeIndexHere)));
                newNodeIndex++;
            }
            Task.WaitAll(tasks.ToArray());

            Resolve();
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

        private void Resolve()
        {
            throw new NotImplementedException();
        }

        public class FamilyNotClusteredException : Exception { }
    }
}
