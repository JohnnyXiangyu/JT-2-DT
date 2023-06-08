using JT_2_DT.Utils;
using System.Diagnostics;

namespace JT_2_DT
{
	public class Dtree
	{
		public HashSet<int> Leaves { get; private set; } = new HashSet<int>();

		private List<HashSet<int>> _edges = new();
		private List<HashSet<int>> _clusterMapping = new();
		private readonly Dictionary<int, int> _nodeToClause = new();
		private int _nodeCount;
		private int _rootByConvention;

		// clean build related
		private readonly bool _cleanBuild;
		List<FamilyEquivalenceClass> _familyEqClasses = new();
		Dictionary<int, FamilyEquivalenceClass> _clauseToEqClass = new();
		
		Dictionary<int, int> _childernToParent = new();
		Dictionary<int, List<int>> _parentToChildren = new();

		public Dtree(string filePath, IEnumerable<IEnumerable<int>> families, bool useCleanCompiler = false)
		{
			LoadTreeDecompFile(filePath);		
			
			_cleanBuild = useCleanCompiler;
			if (!useCleanCompiler)
			{
				MakeDirtyDtree(families);
			}
			else
			{
				MakeCleanDtree(families);
			}
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
				if (line.Length == 0) 
				{
					continue;
				}
				
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
							var words = line.TrimEnd().Split(' ');
							int clusterIndex = int.Parse(words[1]) - 1;

							for (int i = 2; i < words.Length; i++)
							{
								// variables start with 1
								_clusterMapping[clusterIndex].Add(int.Parse(words[i]));
							}
							
							Leaves.Add(clusterIndex);

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
			_edges[v1].Add(v2);
			_edges[v2].Add(v1);

			GlobalLeafCheck(v1);
			GlobalLeafCheck(v2);
		}

		/// <summary>
		/// Fully delete a node and from the graph.
		/// </summary>
		/// <param name="node">the node to delete</param>
		/// <returns>new leaves created by removing this node</returns>
		private HashSet<int> BanishNode(int node)
		{
			HashSet<int> targets;
			targets = new(_edges[node]);

			HashSet<int> newLeaves = new();
			foreach (int v2 in targets)
			{
				_edges[node].Remove(v2);
				_edges[v2].Remove(node);

				if (GlobalLeafCheck(v2))
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
		private void PurgeLeavesInRange(HashSet<int> range, HashSet<int> protectedLeaves)
		{
			Stack<int> stack = new(range.Intersect(Leaves));
			while (stack.Any())
			{
				int nextLeaf = stack.Pop();

				HashSet<int> newLeaves = BanishNode(nextLeaf);
				foreach (int leaf in newLeaves)
				{
					if (protectedLeaves.Contains(leaf)) 
					{
						continue;
					}
					
					stack.Push(leaf);
				}
			}
		}

		/// <summary>
		/// Convert the tree decomposition graph into a dtree, with the given families.
		/// </summary>
		/// <param name="families"></param>
		private void MakeDirtyDtree(IEnumerable<IEnumerable<int>> families)
		{
			HashSet<int> oldLeaves = new(Leaves);

			foreach (var fam in families)
			{
				_clusterMapping.Add(new(fam));
			}
			ExtendNode(families.Count());
			
			HashSet<int> protectedNodes = new();
			int index = 0;
			foreach (IEnumerable<int> fam in families)
			{
				// index := index of clause
				// fam := the family created from this clause (reduced to a hashset)
				int newNodeIndex = _nodeCount + index;
				InsertFamily(fam, newNodeIndex);
				_nodeToClause[newNodeIndex] = index;
				protectedNodes.Add(newNodeIndex);
				index++;
			}
			
			// purge out useless leaves
			PurgeLeavesInRange(oldLeaves, protectedNodes);
			
			// finalize insertion by updating node count
			_nodeCount += families.Count();

			// last step is to resolve the tree to ensure it's a full binary tree
			_rootByConvention = ResolveAsBinaryTree();
		}

		/// <summary>
		/// Convert the loaded tree decomposition to dtree, but remove all subsumed families.
		/// Removed families are added back during serialziation.
		/// </summary>
		/// <param name="families">families from CNF, including all subsumed</param>
		private void MakeCleanDtree(IEnumerable<IEnumerable<int>> families)
		{
			HashSet<int> oldLeaves = new(Leaves);

			// create a clean list of families
			int currentClause = 0;
			foreach (var fam in families)
			{
				bool included = false;
				HashSet<int> famSet = new(fam);
				for (int i = 0; i < _familyEqClasses.Count; i++)
				{
					if (_familyEqClasses[i].MasterFamily.IsSubsetOf(famSet)) 
					{
						_familyEqClasses[i].SubsumedClauses.Add(currentClause);
						included = true;
						break;
					}
					else if (famSet.IsSupersetOf(_familyEqClasses[i].MasterFamily)) 
					{
						_familyEqClasses[i].SubsumedClauses.Add(_familyEqClasses[i].MasterClause);
						_familyEqClasses[i].MasterClause = currentClause;
						_familyEqClasses[i].MasterFamily = famSet;
						included = true;
						break;
					}
				}

				if (!included)
				{					
					_familyEqClasses.Add(new() 
					{
						MasterFamily = new(fam),
						SubsumedClauses = new(),
						MasterClause = currentClause
					});
				}

				currentClause++;
			}

			// make room for families after purge
			foreach (var ec in _familyEqClasses) 
			{
				_clusterMapping.Add(ec.MasterFamily);
				_clauseToEqClass[ec.MasterClause] = ec;
			}
			ExtendNode(_familyEqClasses.Count);

			// insert most inclusive families only
			HashSet<int> protectedNodes = new();
			for (int index = 0; index < _familyEqClasses.Count; index++)
			{
				var fam = _familyEqClasses[index].MasterFamily;
				int newNodeIndex = _nodeCount + index;
				InsertFamily(fam, newNodeIndex);
				_nodeToClause[newNodeIndex] = _familyEqClasses[index].MasterClause;
				protectedNodes.Add(newNodeIndex);
			}

			// purge out useless leaves
			PurgeLeavesInRange(oldLeaves, protectedNodes);

			// finalize insertion by updating node count
			_nodeCount += _familyEqClasses.Count;

			// last step is to resolve the tree to ensure it's a full binary tree
			_rootByConvention = ResolveAsBinaryTree();
		}


		int _currentStart =0;
		/// <summary>
		/// Insert a new node that originates from a family from the source CNF.
		/// </summary>
		/// <param name="family">a set of variables</param>
		/// <param name="newIndex">the node to which this family is inserted</param>
		private void InsertFamily(IEnumerable<int> family, int newIndex)
		{
			for (int i = 0; i < _nodeCount; i++)
			{
				int index = (_currentStart + i) % _nodeCount;
				
				if (_clusterMapping[index].IsSupersetOf(family))
				{
					AddEdge(index, newIndex);
					_currentStart = index;
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
			
			Dictionary<int, int> parentVisitCount = new();
			foreach (int leaf in Leaves) 
			{
				parentVisitCount[leaf] = 0;
			}

			int conventionalRoot = -1;
			
			while (pendingNodes.Any())
			{
				int currentLeaf = pendingNodes.SafeDequeue();
				bool isRoot = !_edges[currentLeaf].Except(processedNodes).Any();
				int parent;
				if (!isRoot)
				{
					parent = _edges[currentLeaf].Except(processedNodes).FirstOrDefault();
					if (!parentVisitCount.ContainsKey(parent)) 
					{
						parentVisitCount[parent] = 0;
					}
				}
				else
				{
					parent = -1;
				}

				var children = _edges[currentLeaf].Intersect(processedNodes);

				if (children.Count() == 1)
				{
					foreach (int child in children)
					{
						// detatch this node from the tree
						_edges[currentLeaf].Remove(child);
						_edges[child].Remove(currentLeaf);

						if (!isRoot)
						{
							_edges[parent].Remove(currentLeaf);
							_edges[currentLeaf].Remove(parent);
							AddEdge(child, parent);
						}
						
						if (!isRoot) 
						{
							_childernToParent[child] = parent;
							parentVisitCount[parent] ++;
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
					
					if (!isRoot) 
					{
						_childernToParent[currentLeaf] = parent;
						parentVisitCount[parent] ++;
					}
				}

				// register the node to processed nodes
				processedNodes.Add(currentLeaf);

				if (!isRoot)
				{
					if (_edges[parent].Count - parentVisitCount[parent] <= 1)
					{
						pendingNodes.SafeEnqueue(parent);
					}
				}
				else
				{
					conventionalRoot = currentLeaf;
				}
			}
			
			// build mapping from parent to children
			foreach ((int child, int parent) in _childernToParent) 
			{
				if (!_parentToChildren.ContainsKey(parent)) 
				{
					_parentToChildren[parent] = new();
				}
				_parentToChildren[parent].Add(child);
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
			_childernToParent[newIntermediate] = target;

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
					_childernToParent[child] = newIntermediate;
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
		public IEnumerable<string> SerializeAsDtree()
		{
			// start with bottom-up traversal states
			UniqueQueue<int> pendingNodes = new();
			HashSet<int> processedNodes = new();
			Dictionary<int, int> graphNodeToSerializeNode = new();
			int[] childVisitCount = new int[_nodeCount];

			// only for clean build
			HashSet<int> subsumingNodes = new();

			// initialize the result list
			List<string> result = new()
			{
				string.Empty
			};

			// helper functions
			void AddToResult(string newLine) => result.Add(newLine);
			
			void TryPushParent(int node)
			{
				if (node == _rootByConvention)
					return;
				
				int paernt = _childernToParent[node];
				
				List<int> allSiblings = _parentToChildren[paernt];
				if (processedNodes.IsSupersetOf(allSiblings)) 
				{
					pendingNodes.SafeEnqueue(paernt);
				}
			}

			// first, lay out all leaves
			foreach (var node in Leaves)
			{
				graphNodeToSerializeNode[node] = result.Count - 1;
				processedNodes.Add(node);

				// find potential parents
				TryPushParent(node);

				int clause = _nodeToClause[node];
				AddToResult($"L {clause}");

				// only for clean build: 
				if (_cleanBuild && _clauseToEqClass.ContainsKey(clause))
				{ // write all subsumed leaves out in this small cluster
					subsumingNodes.Add(node);
					foreach (int subsumedClause in _clauseToEqClass[clause].SubsumedClauses)
					{
						AddToResult($"L {subsumedClause}");
					}
				}
			}

			// only for clean builds, process intermediate nodes that incorporate subsumed nodes
			if (_cleanBuild)
			{
				foreach (int node in subsumingNodes)
				{
					int subsumingClause = _nodeToClause[node];
					List<int> subsumedClauses = _clauseToEqClass[subsumingClause].SubsumedClauses;

					int aggregatedNodeSerialized = graphNodeToSerializeNode[node];
					for (int i = 1; i <= subsumedClauses.Count; i++)
					{
						int subsumedNodeSerialized = graphNodeToSerializeNode[node] + i;
						int newAggregate = result.Count - 1;
						AddToResult($"I {aggregatedNodeSerialized} {subsumedNodeSerialized}");
						aggregatedNodeSerialized = newAggregate;
					}

					graphNodeToSerializeNode[node] = aggregatedNodeSerialized;
				}
			}

			// finally, process all other intermediate nodes
			while (pendingNodes.Any())
			{
				int currentNode = pendingNodes.SafeDequeue();
				processedNodes.Add(currentNode);

				// update mapping
				graphNodeToSerializeNode[currentNode] = result.Count - 1;

				// its intermediate node line
				List<int> children = _parentToChildren[currentNode];
				AddToResult($"I {graphNodeToSerializeNode[children[0]]} {graphNodeToSerializeNode[children[1]]}");
				
				// add parent
				TryPushParent(currentNode);
			}

			result[0] = $"dtree {result.Count - 1}";
			return result.ToArray();
		}

		/// <summary>
		/// Verify if the given node is a leaf or not.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private bool GlobalLeafCheck(int node)
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
