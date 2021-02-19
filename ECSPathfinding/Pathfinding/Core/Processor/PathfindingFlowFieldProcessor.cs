﻿using UnityEngine;

namespace ME.ECS.Pathfinding {
    
    using ME.ECS.Collections;

    public class PathfindingFlowFieldProcessor : PathfindingProcessor {
        
        public override Path Run<TMod>(LogLevel pathfindingLogLevel, Vector3 from, Vector3 to, Constraint constraint, Graph graph, TMod pathModifier, int threadIndex = 0) {

            if (threadIndex < 0) threadIndex = 0;
            threadIndex = threadIndex % Pathfinding.THREADS_COUNT;

            var constraintStart = constraint;
            constraintStart.checkWalkability = true;
            constraintStart.walkable = true;
            var startNode = graph.GetNearest(from, constraintStart);
            if (startNode == null) return new Path();

            var constraintEnd = constraintStart;
            constraintEnd.checkArea = true;
            constraintEnd.areaMask = (1 << startNode.area);
            
            var endNode = graph.GetNearest(to, constraintEnd);
            if (endNode == null) return new Path();
            
            var visited = PoolListCopyable<Node>.Spawn(10);
            System.Diagnostics.Stopwatch swPath = null;
            if ((pathfindingLogLevel & LogLevel.Path) != 0) swPath = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < graph.nodes.Count; ++i) {

                graph.nodes[i].Reset(threadIndex);

            }
            
            this.CreateIntegrationField(graph, visited, endNode, constraint, threadIndex);
            var flowField = PoolArray<byte>.Spawn(graph.nodes.Count);
            this.CreateFlowField(graph, ref flowField, endNode, constraint, threadIndex);

            var statVisited = visited.Count;
            
            var path = new Path();
            path.graph = graph;
            path.result = PathCompleteState.Complete;
            path.flowField = flowField;
            
            for (int i = 0; i < visited.Count; ++i) {

                visited[i].Reset(threadIndex);

            }

            PoolListCopyable<Node>.Recycle(ref visited);

            if ((pathfindingLogLevel & LogLevel.Path) != 0) {
                
                Logger.Log(string.Format("Path result {0}, built in {1}ms. Path length: (visited: {2})\nThread Index: {3}", path.result, swPath.ElapsedMilliseconds, statVisited, threadIndex));
                
            }

            return path;

        }

        private void CreateFlowField(Graph graph, ref BufferArray<byte> flowField, Node endNode, Constraint constraint, int threadIndex) {

            // Create flow field
            for (int i = 0, cnt = graph.nodes.Count; i < cnt; ++i) {

                var ffCost = graph.nodes[i].bestCost[threadIndex];
                var node = graph.nodes[i];
                var minCost = ffCost;
                if (endNode == node) minCost = 0;

                var connections = node.GetConnections();
                var dir = 0;
                var iterDir = 0;
                foreach (var neighbour in connections) {
                    
                    ++iterDir;

                    if (neighbour.index < 0) continue;
                    
                    var idx = neighbour.index;
                    var cost = graph.nodes[idx].bestCost[threadIndex];

                    if (cost < minCost) {

                        minCost = cost;
                        dir = iterDir - 1;

                    }

                }

                flowField.arr[i] = (byte)dir;

            }
            
        }
        
        private void CreateIntegrationField(Graph graph, ListCopyable<Node> visited, Node endNode, Constraint constraint, int threadIndex) {

            // Create integration field
            var queue = PoolQueue<Node>.Spawn(500);
            queue.Enqueue(endNode);

            endNode.bestCost[threadIndex] = 0;
            visited.Add(endNode);

            while (queue.Count > 0) {

                var curNode = queue.Dequeue();
                var connections = curNode.GetConnections();
                for (int i = 2; i <= 5; ++i) {

                    var conn = connections[i];
                    if (conn.index < 0) continue;

                    var neighbor = graph.nodes[conn.index];
                    if (neighbor.IsSuitable(constraint) == false) {

                        continue;

                    }

                    var cost = neighbor.penalty;
                    var endNodeCost = (ushort)(cost + curNode.bestCost[threadIndex]);
                    if (endNodeCost < neighbor.bestCost[threadIndex]) {

                        neighbor.bestCost[threadIndex] = endNodeCost;
                        queue.Enqueue(neighbor);
                        visited.Add(neighbor);

                    }

                }

            }
            
            PoolQueue<Node>.Recycle(ref queue);

        }
        
    }

}
