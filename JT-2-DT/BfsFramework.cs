using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace JT_2_DT
{
    /// <summary>
    /// Helper framework to parallelize bfs without breaking the nice memory behavior.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class BfsFramework<T>
    {
        public static void Search(T startNode, Func<T, IEnumerable<T>> search)
        {
            BfsFramework<T>.SearchAsync(startNode, search).Wait();
        }

        public static async Task SearchAsync(T startNode, Func<T, IEnumerable<T>> search)
        {
            Queue<T> bfsFrame = new();
            bfsFrame.Enqueue(startNode);

            while (bfsFrame.Count > 0)
            {
                List<Task<IEnumerable<T>>> tasks = new();
                
                while (bfsFrame.Count > 0)
                {
                    var nextChild = bfsFrame.Dequeue();
                    tasks.Add(Task.Run(() =>
                    {
                        return search(nextChild);
                    }));
                }

                foreach (var task in tasks)
                {
                    var collection = await task;
                    foreach (T item in collection)
                    {
                        bfsFrame.Enqueue(item);
                    }
                }
            }
        }
    }
}
