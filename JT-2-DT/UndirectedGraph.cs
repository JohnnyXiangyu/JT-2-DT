namespace JT_2_DT
{
    internal class UndirectedGraph
    {
        private readonly HashSet<int>[] _edges; 

        public UndirectedGraph(int vertexCount)
        {
            _edges = new HashSet<int>[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                _edges[i] = new();
            }
        }

        // thread safe
        public void AddEdge(int v1, int v2)
        {
            int smaller = Math.Min(v1, v2);
            int bigger = Math.Max(v1, v2);

            lock (_edges[smaller])
            {

                _edges[smaller].Add(bigger);
            }
        }

        // thread safe
        public void RemoveEdge(int v1, int v2)
        {
            int smaller = Math.Min(v1, v2);
            int bigger = Math.Max(v1, v2);
            
            lock (_edges[smaller])
            {
                _edges[smaller].Remove(bigger);
            }
        }
    }
}
