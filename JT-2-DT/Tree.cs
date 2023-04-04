using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JT_2_DT
{
    internal class Tree
    {
        public Tree? Parent { get; set; }
        public List<Tree> Children { get; set; } = new();
        public HashSet<int> Cluster { get; set; } = new();

        public static void Connect(Tree parent, Tree child)
        {
            parent.Children.Add(child);
            child.Parent = parent;
        }

        public void AddChildren(IEnumerable<Tree> newChildren)
        {
            foreach (var child in newChildren)
            {
                Children.Add(child);
            }
        }

        public bool ContainsInCluster(HashSet<int> family)
        {
            return family.IsSubsetOf(Cluster);
        }
        
        public bool IsJoinTree(IEnumerable<HashSet<int>> families)
        {
            throw new NotImplementedException();
        }

        public bool IsDTree(IEnumerable<HashSet<int>> families)
        {
            throw new NotImplementedException();
        }

        public int JoinTreeWidth()
        {
            int result = 0;

            foreach (var child in Children)
            {
                if (child.Cluster.Count > result)
                {
                    result = child.Cluster.Count;
                }
            }

            return result;
        }

        public int DTreeWidth()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Recursively resolve the entire tree rooted at this node.
        /// </summary>
        /// <returns>the root of the resolved tree</returns>
        private void Resolve()
        {
            foreach (var child in Children)
            {
                child.Resolve();
            }

            if (Children.Count == 1)
            {
                TakeOver();
            }

            Tree target = this;
            while (target.Children.Count > 2)
            {
                target = target.Extend();
            }
        }

        /// <summary>
        /// Extend this node by choosing 1 child, move every other child into a new node, and make that new node (extension) this node's second child.
        /// </summary>
        /// <returns>the extension node</returns>
        private Tree Extend()
        {
            var leftChild = Children[0];
            Tree extension = new();
            
            foreach (var child in Children.Where(x => x != leftChild))
            {
                Connect(extension, child);
            }

            Children.Clear();
            Children.Add(leftChild);
            Children.Add(extension);

            return extension;
        }

        /// <summary>
        /// Make sure every reference kept in this class is gone so GC will kick in later.
        /// </summary>
        private void Decompose()
        {
            Children.Clear();
            Parent = null;
        }

        /// <summary>
        /// Connect the only child's children to this node, and remove the only original child.
        /// </summary>
        /// <exception cref="Exception">when there's not exactly one child in this node</exception>
        private void TakeOver()
        {
            if (Children.Count != 1) // error
            {
                throw new Exception("collapsing when not exactly one child left");
            }

            // take over the only child
            var taker = Children[0];
            Children.Clear();
            foreach (var child in taker.Children)
            {
                Connect(this, child);
            }
            taker.Decompose();
        }
    }
}
