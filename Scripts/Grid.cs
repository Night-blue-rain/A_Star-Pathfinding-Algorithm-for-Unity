using System.Collections.Generic;
using UnityEngine;

namespace A_Star.Scripts
{
   public class Grid : MonoBehaviour
    {
        public static Grid Singleton; 
        
        public Vector2 gridWorldSize; // 网格的世界大小
        public float nodeRadius; // 节点半径
        
        private Node[,] grid; // 二维节点数组
        private float nodeDiameter;
        [HideInInspector] public int gridSizeX;
        [HideInInspector] public int gridSizeY;
        
        public float raycastStartHeight = 50f; // 射线检测的“起始高度”（需高于地形最高处，避免漏检）
        public LayerMask terrainMask; // 射线检测的初始地形
        public LayerMask unwalkableMask; // 不可行走的图层
        public float maxClimbHeight = 0.5f; // 最大可直接攀爬的高度差（如台阶）
        public float maxSlopeAngle = 45f; // 最大可攀爬坡度（度）
        private float maxSlopeTan; // 坡度的正切值（用于计算）

        void Awake()
        {
            Singleton = this;
            
            nodeDiameter = nodeRadius * 2;
            gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
            gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
            maxSlopeTan = Mathf.Tan(maxSlopeAngle * Mathf.Deg2Rad); // 转换为弧度计算正切值
            CreateGrid();
        }
        
        void CreateGrid() 
        {
            grid = new Node[gridSizeX, gridSizeY];
            Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;
            
            for (int x = 0; x < gridSizeX; x++) 
            {
                for (int y = 0; y < gridSizeY; y++) 
                {
                    // 1. 计算节点在 X/Z 平面的位置（Y 轴先设为射线起始高度）
                    float nodeX = worldBottomLeft.x + x * nodeDiameter + nodeRadius;
                    float nodeZ = worldBottomLeft.z + y * nodeDiameter + nodeRadius;
                    Vector3 rayStartPos = new Vector3(nodeX, raycastStartHeight, nodeZ);

                    // 2. 射线向下检测地形表面（只检测指定的 terrainMask）
                    if (Physics.Raycast(rayStartPos, Vector3.down, out RaycastHit hit, raycastStartHeight*2, terrainMask))
                    {
                        // 3. 射线命中地形：节点位置设为命中点（贴合地形表面）
                        Vector3 nodeWorldPos = hit.point;
                        // 4. 检测该节点是否“可行走”（避免地形凸起/障碍物，复用原碰撞检测逻辑）
                        bool walkable = !(Physics.CheckSphere(nodeWorldPos, Mathf.Max(0.1f,nodeRadius-0.25f), unwalkableMask));
                        // 5. 创建节点并存入网格
                        grid[x, y] = new Node(walkable, nodeWorldPos, new Vector2Int(x, y));
                    }
                    else
                    {
                        // 射线未命中地形（如网格超出地形范围）：标记为不可行走
                        Vector3 emptyPos = new Vector3(nodeX, 0, nodeZ); // 悬空位置
                        grid[x, y] = new Node(false, emptyPos, new Vector2Int(x, y));
                    }
                    
                }
            }
            
            // 节点创建完成后，计算每个节点与邻居的可攀爬性
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    Node node = grid[x, y];
                    if (!node.walkable) continue;

                    // 检查所有邻居节点
                    foreach (Node neighbor in GetNeighbours(node))
                    {
                        if (!neighbor.walkable) continue;

                        // 计算高度差和水平距离
                        float heightDiff = Mathf.Abs(neighbor.height - node.height);
                        float horizontalDist = Vector3.Distance(
                            new Vector3(node.worldPosition.x, 0, node.worldPosition.z),
                            new Vector3(neighbor.worldPosition.x, 0, neighbor.worldPosition.z)
                        );

                        // 水平距离过小（几乎重叠）直接跳过
                        if (horizontalDist < 0.01f) continue;

                        // 计算坡度（高度差/水平距离 = tanθ）
                        float slope = heightDiff / horizontalDist;

                        // 如果高度差超过最大可攀爬高度，或坡度超过最大角度，则不可攀爬
                        if (heightDiff > maxClimbHeight && slope > maxSlopeTan)
                        {
                            node.climbable = false;
                            break; // 只要有一个邻居不可达，当前节点就标记为不可攀爬
                        }
                    }
                }
            }
        }
        
        // 获取节点周围的邻居节点
        public List<Node> GetNeighbours(Node node) {
            List<Node> neighbours = new List<Node>();
            
            for (int x = -1; x <= 1; x++) {
                for (int y = -1; y <= 1; y++) {
                    if (x == 0 && y == 0)
                        continue; // 跳过自身
                    
                    int checkX = node.gridPosition.x + x;
                    int checkY = node.gridPosition.y + y;
                    
                    // 检查是否在网格范围内
                    if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) {
                        neighbours.Add(grid[checkX, checkY]);
                    }
                }
            }
            
            return neighbours;
        }
        
        // 将世界位置转换为网格节点
        public Node NodeFromWorldPoint(Vector3 worldPosition) {
            float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
            float percentY = (worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y;
            percentX = Mathf.Clamp01(percentX);
            percentY = Mathf.Clamp01(percentY);
            
            int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
            int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
            return grid[x, y];
        }
        
        // 只更新指定位置周围的局部网格
        public void UpdateGridArea(Vector3 centerPos, float range)
        {
            // 1. 计算“需要更新的节点范围”（将世界坐标转网格坐标）
            Node centerNode = NodeFromWorldPoint(centerPos);
            int rangeInNodes = Mathf.RoundToInt(range / nodeDiameter); // 范围转节点数

            // 2. 只遍历“中心节点±rangeInNodes”的局部区域
            for (int x = -rangeInNodes; x <= rangeInNodes; x++)
            {
                for (int y = -rangeInNodes; y <= rangeInNodes; y++)
                {
                    int checkX = centerNode.gridPosition.x + x;
                    int checkY = centerNode.gridPosition.y + y;
                    // 确保坐标在网格范围内
                    if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                    {
                        // 重新通过射线检测获取当前地形高度
                        Vector3 rayStartPos = new Vector3(grid[checkX, checkY].worldPosition.x, raycastStartHeight, grid[checkX, checkY].worldPosition.z);
                        if (Physics.Raycast(rayStartPos, Vector3.down, out RaycastHit hit, raycastStartHeight * 2, terrainMask))
                        {
                            // 更新节点位置到地形表面，并重新检测是否可行走
                            Vector3 newNodeWorldPos = hit.point;
                            bool isObstacle = Physics.CheckSphere(newNodeWorldPos, nodeRadius, unwalkableMask);
                            grid[checkX, checkY].worldPosition = newNodeWorldPos; // 更新高度
                            grid[checkX, checkY].walkable = !isObstacle;
                        }
                        else
                        {
                            // 超出地形范围，标记为不可行走
                            grid[checkX, checkY].walkable = false;
                        }
                    }
                }
            }
            
            // 重新计算更新区域内节点的可攀爬性
            for (int x = -rangeInNodes; x <= rangeInNodes; x++)
            {
                for (int y = -rangeInNodes; y <= rangeInNodes; y++)
                {
                    int checkX = centerNode.gridPosition.x + x;
                    int checkY = centerNode.gridPosition.y + y;
                    if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                    {
                        Node node = grid[checkX, checkY];
                        if (!node.walkable) continue;

                        // 重置为可攀爬，重新检测
                        node.climbable = true;
                        foreach (Node neighbor in GetNeighbours(node))
                        {
                            if (!neighbor.walkable) continue;

                            float heightDiff = Mathf.Abs(neighbor.height - node.height);
                            float horizontalDist = Vector3.Distance(
                                new Vector3(node.worldPosition.x, 0, node.worldPosition.z),
                                new Vector3(neighbor.worldPosition.x, 0, neighbor.worldPosition.z)
                            );

                            if (horizontalDist < 0.01f) continue;
                            float slope = heightDiff / horizontalDist;

                            if (heightDiff > maxClimbHeight && slope > maxSlopeTan)
                            {
                                node.climbable = false;
                                break;
                            }
                        }
                    }
                }
            }
            
        }

        // 用于获取网格数据（供可视化脚本使用）
        public Node[,] GetGridArray() {
            return grid;
        }
        
        // 编辑模式下始终显示网格边界（无论是否选中物体）
        void OnDrawGizmos() {
            // 绘制网格整体边界（一个矩形）
            Gizmos.color = Color.cyan;
            Vector3 center = transform.position;
            // 计算矩形的大小（Y轴高度设为0.1，避免与地面重叠）
            Vector3 size = new Vector3(gridWorldSize.x, 0.1f, gridWorldSize.y);
            Gizmos.DrawWireCube(center, size);
        }
        
    }
}
    
    