using UnityEngine;

namespace A_Star.Scripts
{
    public class Node
    {
        public bool walkable; // 是否可通行
        public bool climbable; // 是否可从相邻节点攀爬到达（考虑高度差）
        public float height; // 节点高度（Y轴）
        public Vector2Int gridPosition; // 网格位置
        public Vector3 worldPosition; // 世界位置
    
        public float gCost; // 起点到当前节点的代价
        public float hCost; // 当前节点到终点的估计代价（欧几里得距离）
        public Node parent; // 父节点
    
        // F值是G值和H值的总和
        public float fCost {
            get { return gCost + hCost; }
        }
    
        public Node(bool walkable, Vector3 worldPos, Vector2Int gridPos)
        {
            this.walkable = walkable;
            this.worldPosition = worldPos;
            this.gridPosition = gridPos;
            this.height = worldPos.y; // 从世界位置提取高度
            this.climbable = true; // 默认可攀爬，后续会重新计算
        }
    }
}