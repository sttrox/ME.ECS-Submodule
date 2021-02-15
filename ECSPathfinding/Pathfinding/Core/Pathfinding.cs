﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ME.ECS.Pathfinding {
    
    using ME.ECS.Collections;
    using Unity.Jobs;

    public struct PathTask {

        public Entity entity;
        public Vector3 from;
        public Vector3 to;
        public Constraint constraint;
        public PathCornersModifier pathCornersModifier;
        public bool isValid;

    }
    
    [ExecuteInEditMode]
    #if ECS_COMPILE_IL2CPP_OPTIONS
        [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.DivideByZeroChecks, false)]
    #endif
    public class Pathfinding : MonoBehaviour {

        #if ECS_COMPILE_IL2CPP_OPTIONS
        [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
        [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.DivideByZeroChecks, false)]
        #endif
        [System.Serializable]
        public sealed class ModificatorItem {

            public bool enabled;
            public GraphModifierBase modifier;
            
        }

        public const int THREADS_COUNT = 8;

        public PathfindingProcessor processor = new PathfindingProcessor();
        public List<Graph> graphs;

        public LogLevel logLevel;

        public bool clonePathfinding = false;
        
        private HashSet<GraphDynamicModifier> dynamicModifiersContains = new HashSet<GraphDynamicModifier>();
        private HashSet<GraphDynamicModifier> dynamicModifiersList = new HashSet<GraphDynamicModifier>();

        private struct CopyGraph : IArrayElementCopy<Graph> {

            public void Copy(Graph @from, ref Graph to) {
                
                to.CopyFrom(from);
                
            }

            public void Recycle(Graph item) {

                item.Recycle();
                
            }

        }

        public Pathfinding Clone() {

            var instance = Object.Instantiate(this);
            for (int i = 0; i < this.graphs.Count; ++i) {

                this.graphs[i].pathfindingLogLevel = instance.logLevel;

            }
            instance.CopyFrom(this);
            return instance;

        }

        public void Recycle() {

            this.OnRecycle();
            
            if (this != null && this.gameObject != null) {
                
                Object.Destroy(this.gameObject);
                
            }
            
        }

        private void OnRecycle() {

            if (this.graphs != null) {

                for (int i = 0; i < this.graphs.Count; ++i) {

                    this.graphs[i].Recycle();

                }
                
                PoolList<Graph>.Recycle(ref this.graphs);
                
            }

        }
        
        public void CopyFrom(Pathfinding other) {

            this.processor = other.processor;
            this.logLevel = other.logLevel;

            ArrayUtils.Copy(other.graphs, ref this.graphs, new CopyGraph());
            
        }
        
        public bool HasLogLevel(LogLevel level) {

            return (this.logLevel & level) != 0;

        }
        
        public void RegisterDynamic(GraphDynamicModifier modifier) {

            if (this.dynamicModifiersContains.Contains(modifier) == false) {

                if (this.dynamicModifiersContains.Add(modifier) == true) {

                    this.dynamicModifiersList.Add(modifier);
                    modifier.ApplyForced();
                    this.BuildAreas();
                    
                }
                
            }

        }

        public void UnRegisterDynamic(GraphDynamicModifier modifier) {

            if (this.dynamicModifiersContains.Contains(modifier) == true) {

                if (this.dynamicModifiersContains.Remove(modifier) == true) {

                    this.dynamicModifiersList.Remove(modifier);
                    modifier.ApplyForced(disabled: true);
                    this.BuildAreas();

                }
                
            }
            
        }

        public void AdvanceTick(float deltaTime) {

            var anyUpdated = false;
            foreach (var mod in this.dynamicModifiersList) {
                
                anyUpdated |= mod.Apply();
                
            }

            if (anyUpdated == true) {
                
                this.BuildAreas();

            }
            
        }

        public Node GetNearest(Vector3 worldPosition) {

            return this.GetNearest(worldPosition, Constraint.Default);

        }

        public Node GetNearest(Vector3 worldPosition, Constraint constraint) {

            Node nearest = null;
            if (this.graphs != null) {

                float dist = float.MaxValue;
                for (int i = 0; i < this.graphs.Count; ++i) {

                    if (constraint.graphMask >= 0 && (constraint.graphMask & (1 << this.graphs[i].index)) == 0) continue;
                    
                    var node = this.graphs[i].GetNearest(worldPosition, constraint);
                    if (node == null) continue;
                    
                    var d = (node.worldPosition - worldPosition).sqrMagnitude;
                    if (d < dist) {

                        dist = d;
                        nearest = node;

                    }

                }

            }

            return nearest;

        }

        public Path CalculatePath(Vector3 from, Vector3 to) {

            var constraint = Constraint.Default;
            return this.CalculatePath(from, to, constraint);
            
        }

        public Path CalculatePath(Vector3 from, Vector3 to, Constraint constraint) {

            return this.CalculatePath(from, to, constraint, new PathModifierEmpty());
            
        }

        public Path CalculatePath<TMod>(Vector3 from, Vector3 to, TMod pathModifier) where TMod : IPathModifier {

            var constraint = Constraint.Default;
            return this.CalculatePath(from, to, constraint, pathModifier);
            
        }

        public Path CalculatePath<TMod>(Vector3 from, Vector3 to, Constraint constraint, TMod pathModifier, int threadIndex = 0) where TMod : IPathModifier {

            var graph = this.GetNearest(from, constraint).graph;
            return this.CalculatePath(from, to, constraint, graph, pathModifier, threadIndex);
            
        }

        public Path CalculatePath<TMod>(Vector3 from, Vector3 to, Constraint constraint, Graph graph, TMod pathModifier, int threadIndex = 0) where TMod : IPathModifier {

            return this.processor.Run(this.logLevel, from, to, constraint, graph, pathModifier, threadIndex);

        }

        public void BuildAreas() {
            
            if (this.graphs != null) {

                for (int i = 0; i < this.graphs.Count; ++i) {

                    this.graphs[i].BuildAreas();

                }

            }
            
        }
        
        public bool BuildNodePhysics(Node node) {
            
            return node.graph.BuildNodePhysics(node);
            
        }
        
        public void GetNodesInBounds(ListCopyable<Node> result, Bounds bounds) {
            
            if (this.graphs != null) {

                for (int i = 0; i < this.graphs.Count; ++i) {

                    this.graphs[i].GetNodesInBounds(result, bounds);

                }

            }
            
        }
        
        public void BuildAll() {

            if (this.graphs != null) {

                for (int i = 0; i < this.graphs.Count; ++i) {

                    this.graphs[i].DoBuild();

                }

            }

        }

        public void OnDrawGizmos() {

            if (this.graphs != null) {

                for (int i = 0; i < this.graphs.Count; ++i) {

                    this.graphs[i].DoDrawGizmos();

                }

            }

        }

        private struct RunTasksJob : Unity.Jobs.IJobParallelFor {

            public Unity.Collections.NativeArray<PathTask> arr;

            void Unity.Jobs.IJobParallelFor.Execute(int index) {

                var item = this.arr[index];
                if (item.isValid == true) Pathfinding.results.arr[index] = Pathfinding.pathfinding.CalculatePath(item.@from, item.to, item.constraint, item.pathCornersModifier, index);
                this.arr[index] = item;

            }

        }

        private static Pathfinding pathfinding;
        private static BufferArray<Path> results;
        public void RunTasks(Unity.Collections.NativeArray<PathTask> tasks, ref BufferArray<Path> results) {

            ArrayUtils.Resize(tasks.Length, ref Pathfinding.results);
            
            Pathfinding.pathfinding = this;
            
            var job = new RunTasksJob() {
                arr = tasks,
            };
            var jobHandle = job.Schedule(tasks.Length, 64);
            jobHandle.Complete();

            results = Pathfinding.results;

            /*
            for (int i = 0; i < tasks.Count; ++i) {

                var task = tasks[i];
                task.result = this.CalculatePath(task.@from, task.to, task.constraint, task.pathCornersModifier);
                tasks[i] = task;

            }*/

        }
        
        public PathTask CalculatePathTask(Entity entity, Vector3 requestFrom, Vector3 requestTo, Constraint constraint, PathCornersModifier pathCornersModifier) {

            return new PathTask() {
                entity = entity,
                from = requestFrom,
                to = requestTo,
                constraint = constraint,
                pathCornersModifier = pathCornersModifier,
                isValid = true,
            };
            
        }

    }

}
