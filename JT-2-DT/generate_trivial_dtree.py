total_clauses = int(input())
total_nodes = 2 * total_clauses - 1

print(f'dtree {total_nodes}')

for i in range(total_clauses):
    print(f'L {i}')

current_node = total_clauses
pending_leaves = []
for i in range(total_clauses):
    pending_leaves.append(i)
    if len(pending_leaves) == 2:
        print(f'I {pending_leaves[0]} {pending_leaves[1]}')
        pending_leaves = [current_node]
        current_node += 1
    