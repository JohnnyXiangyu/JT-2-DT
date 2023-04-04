using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JT_2_DT
{
    using Family = HashSet<int>;

    internal class JoinTree
    {
        // connections
        public JoinTree? Parent { get; set; }
        public List<JoinTree> Children { get; set; } = new();

        // settings
        public Family Cluster { get; set; } = new();
        private bool _isFamilyNode = false;

        // properties (real-time)
        public bool IsJoinTree { get { throw new NotImplementedException(); } }
        public bool IsLeaf { get => Children.Count == 0; }
        public int JoinTreeWidth
        {
            get
            {
                int result = Cluster.Count;

                foreach (var child in Children)
                {
                    if (child.Cluster.Count > result)
                    {
                        result = child.Cluster.Count;
                    }
                }

                return result;
            }
        }

        public void MakeDTree(IEnumerable<Family> families)
        {
            List<Task> tasks = new();
            foreach (var family in families)
            {
                tasks.Add(Task.Run(() => 
                {
                    InsertFamily(family);
                }));
            }
        }

        /// <summary>
        /// Recursively add a family into the tree.
        /// </summary>
        /// <param name="fam"></param>
        /// <returns>boolean, success or not</returns>
        private bool InsertFamily(Family fam)
        {
            foreach (var child in Children)
            {
                if (child.InsertFamily(fam))
                {
                    return true;
                }
            }

            if (fam.IsSubsetOf(Cluster))
            {
                JoinTree newNode = new()
                {
                    Cluster = fam,
                    _isFamilyNode = true
                };

                lock (this)
                {
                    Children.Add(newNode);
                }

                return true;
            }

            return false;
        }

        // static tools
        private static void Connect(JoinTree parent, JoinTree child)
        {
            parent.Children.Add(child);
            child.Parent = parent;
        }

        private static void CopySettings(JoinTree target, JoinTree source)
        {
            target._isFamilyNode = source._isFamilyNode;
            target.Cluster = new(source.Cluster);
        }

        /// <summary>
        /// Recursively resolve the entire tree rooted at this node.
        /// Resolve has 3 cases:
        ///     1. leaf node that's not directly a added family node -> remove this node;
        ///     2. non-leaf node with exactly one child -> take over the child;
        ///     3. non-leaf node with more than 2 children -> extend this node until there's no overflow of children.
        /// </summary>
        private void Resolve()
        {
            // for leaves
            if (!_isFamilyNode && Children.Count == 0)
            {
                Parent?.Children.Remove(this);
                Decompose();
                return;
            }

            foreach (var child in Children)
            {
                child.Resolve();
            }

            if (Children.Count == 1)
            {
                TakeOver();
            }

            JoinTree target = this;
            while (target.Children.Count > 2)
            {
                target = target.Extend();
            }
        }

        /// <summary>
        /// Extend this node by choosing 1 child, move every other child into a new node, and make that new node (extension) this node's second child.
        /// </summary>
        /// <returns>the extension node</returns>
        private JoinTree Extend()
        {
            var leftChild = Children[0];
            JoinTree extension = new();
            CopySettings(extension, this);
            
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

            // reconnect children
            var taker = Children[0];
            Children.Clear();
            foreach (var child in taker.Children)
            {
                Connect(this, child);
            }

            // copy settings
            CopySettings(this, taker);

            taker.Decompose();
        }
    }
}
