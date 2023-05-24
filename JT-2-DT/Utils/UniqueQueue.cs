namespace JT_2_DT.Utils
{
    internal class UniqueQueue<T> : Queue<T>
    {
        readonly HashSet<T> _gate = new();

        public UniqueQueue() : base() { }

        public UniqueQueue(IEnumerable<T> source) : base(source) { }

        public T SafeDequeue()
        {
            T result = Dequeue();
            _gate.Remove(result);
            return result;
        }

        public bool SafeEnqueue(T input)
        {
            if (_gate.Contains(input))
            {
                return false;
            }

            Enqueue(input);
            _gate.Add(input);
            return true;
        }
    }
}
