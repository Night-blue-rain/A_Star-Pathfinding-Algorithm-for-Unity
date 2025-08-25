using System.Collections.Generic;
using Priority_Queue;
using UnityEngine;

namespace A_Star.Scripts
{
    public class Pathfinding : MonoBehaviour
    {
        private Grid grid => Grid.Singleton;
        
        // 寻找路径的入口方法
        public List<Node> FindPath(Vector3 startPos, Vector3 targetPos) 
        {
            Node startNode = grid.NodeFromWorldPoint(startPos);
            Node targetNode = grid.NodeFromWorldPoint(targetPos);
            
            // 使用PriorityQueue作为开放列表，按fCost排序
            SimplePriorityQueue<Node, float> openSet = new SimplePriorityQueue<Node, float>();
            HashSet<Node> closedSet = new HashSet<Node>();
            openSet.Enqueue(startNode, startNode.fCost);
            
            while (openSet.Count > 0) 
            {
                // 从优先队列中获取fCost最小的节点
                Node currentNode = openSet.Dequeue();
                closedSet.Add(currentNode);
                
                // 如果到达目标节点，回溯路径
                if (currentNode == targetNode) {
                    return RetracePath(startNode, targetNode);
                }
                
                // 检查所有邻居节点
                foreach (Node neighbour in grid.GetNeighbours(currentNode)) 
                {
                    // 邻居必须可行走，且当前节点到邻居节点可攀爬
                    if (!neighbour.walkable || !neighbour.climbable || closedSet.Contains(neighbour))
                    {
                        continue;
                    }
                    
                    // 计算移动代价（加入高度差代价，使算法优先选择平缓路径）
                    float heightDiff = neighbour.height - currentNode.height;
                    float heightCost = Mathf.Max(0, heightDiff * 2f); // 上坡增加额外代价
                    float newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour) + heightCost;
                    
                    if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour)) 
                    {
                        neighbour.gCost = newMovementCostToNeighbour;
                        // 使用欧几里得距离计算启发值
                        neighbour.hCost = GetEuclideanDistance(neighbour, targetNode);
                        neighbour.parent = currentNode;
                        
                        if (openSet.Contains(neighbour)) {
                            // 如果已在开放列表中，更新优先级
                            openSet.UpdatePriority(neighbour, neighbour.fCost);
                        } else {
                            openSet.Enqueue(neighbour, neighbour.fCost);
                        }
                    }
                }
            }
            
            // 若未找到到目标的路径：从closedSet中找“离目标最近的可达节点”
            if (closedSet.Count > 0)
            {
                Node closestNode = FindClosestNodeToTarget(closedSet, targetNode);
                if (closestNode != null)
                {
                    return RetracePath(startNode, closestNode); // 返回到最近节点的路径
                }
            }
            
            // 极端情况：无任何可达节点（如起点被包围），返回空
            return null;
        }
        
        // 辅助方法：从closedSet中找离目标最近的节点
        private Node FindClosestNodeToTarget(HashSet<Node> closedSet, Node targetNode)
        {
            Node closestNode = null;
            float minDistance = float.MaxValue; // 初始设为最大距离
            
            foreach (Node node in closedSet)
            {
                // 计算节点与目标的直线距离（用欧几里得距离，兼顾XY高度差）
                float distance = GetEuclideanDistance(node, targetNode);
                
                // 若当前节点距离更小，更新最近节点
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestNode = node;
                }
            }
            
            return closestNode;
        }
        
        // 回溯路径
        private List<Node> RetracePath(Node startNode, Node endNode) 
        {
            List<Node> path = new List<Node>();
            Node currentNode = endNode;
            
            while (currentNode != startNode) {
                path.Add(currentNode);
                currentNode = currentNode.parent;
            }
            
            path.Reverse(); // 反转路径，从起点到终点
            return path;
        }
        
        // 计算两个节点之间的实际距离（用于G值）
        private float GetDistance(Node nodeA, Node nodeB) 
        {
            float dstX = Mathf.Abs(nodeA.gridPosition.x - nodeB.gridPosition.x);
            float dstY = Mathf.Abs(nodeA.gridPosition.y - nodeB.gridPosition.y);
            
            // 对角线移动代价优化（1.414≈√2）
            if (dstX > dstY)
                return 1.414f * dstY + dstX - dstY;
            return 1.414f * dstX + dstY - dstX;
        }
        
        // 欧几里得距离（用于H值，直线距离）
        private float GetEuclideanDistance(Node nodeA, Node nodeB) 
        {
            float dx = nodeA.worldPosition.x - nodeB.worldPosition.x;
            float dz = nodeA.worldPosition.z - nodeB.worldPosition.z;
            // 计算三维空间中的直线距离（包含Y轴高度差）
            float dy = nodeA.worldPosition.y - nodeB.worldPosition.y;
            return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}