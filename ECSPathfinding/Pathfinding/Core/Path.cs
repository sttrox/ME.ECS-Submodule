﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ME.ECS.Pathfinding {

    using ME.ECS.Collections;
    
    public enum PathCompleteState {

        NotCalculated = 0,
        Complete,
        NotExist,

    }
 
    public struct Path {

        public PathCompleteState result;
        public Graph graph;
        public ListCopyable<Node> nodes;
        public ListCopyable<Node> nodesModified;
        public BufferArray<byte> flowField;

        public void Recycle() {
            
            if (this.nodes != null) PoolListCopyable<Node>.Recycle(ref this.nodes);
            if (this.nodesModified != null) PoolListCopyable<Node>.Recycle(ref this.nodesModified);
            if (this.flowField.arr != null) PoolArray<byte>.Recycle(ref this.flowField);

        }

    }

}