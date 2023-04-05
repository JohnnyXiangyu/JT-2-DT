// See https://aka.ms/new-console-template for more information
using JT_2_DT;

using Family = System.Collections.Generic.HashSet<int>;

Console.WriteLine("Hello, World!");

JoinTree node_1 = new();
JoinTree node_2 = new();
JoinTree node_3 = new();

// construct the minimal tree
int a = 1;
int b = 2;
int c = 3;
int d = 4;
int e = 5;

node_1.Cluster = new(new int[] { a, b, c });
node_2.Cluster = new(new int[] { b, c, d });
node_3.Cluster = new(new int[] { c, e });

JoinTree.Connect(node_2, node_1);
JoinTree.Connect(node_2, node_3);

Family ab = new(new[] { a, b });
Family ac = new(new[] { a, c });
Family bcd = new(new[] { b, c, d });
Family cd = new(new[] { c, e });

node_2.MakeDTree(new[] {ab, ac, bcd, cd });

node_2.Print();