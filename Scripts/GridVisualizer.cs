using UnityEditor;
using UnityEngine;

namespace A_Star.Scripts
{
    public class GridVisualizer : MonoBehaviour 
    {
        private Grid grid => Grid.Singleton;
        
        public bool showGrid = true; // 是否显示网格
        public bool showWalkable = true; // 是否显示可通行节点
        public bool showUnwalkable = true; // 是否显示不可通行节点
        public bool showNodeGizmos = true; // 是否显示节点详细信息
        public float gizmoSizeMultiplier = 0.9f; // Gizmo大小比例

        public Color walkableColor = Color.white; // 可通行节点颜色
        public Color unwalkableColor = Color.red; // 不可通行节点颜色
        public Color startNodeColor = Color.green; // 起点颜色
        public Color targetNodeColor = Color.blue; // 目标点颜色
        
        private Node[,] nodes;

        // 用于临时显示起点和终点（可在Inspector中设置）
        public Transform debugStart;
        public Transform debugTarget;

        void Update() {
            // 实时获取网格数据（确保参数变化时能实时更新）
            if (grid is not null) 
                nodes = grid.GetGridArray();
        }

        // 在Scene视图中绘制Gizmos
        void OnDrawGizmos() {
            if (grid == null || nodes == null) return;

            if (!showGrid) return;

            // 遍历所有节点并绘制
            for (int x = 0; x < grid.gridSizeX; x++) {
                for (int y = 0; y < grid.gridSizeY; y++) {
                    Node node = nodes[x, y];
                    if (node == null) continue;

                    // 根据节点状态设置颜色
                    Gizmos.color = GetNodeColor(node, x, y);

                    // 绘制节点Gizmo（立方体）
                    Vector3 nodePosition = node.worldPosition;
                    float nodeSize = grid.nodeRadius * 2 * gizmoSizeMultiplier;
                    Gizmos.DrawCube(nodePosition, Vector3.one * nodeSize);

                    // 显示节点的G/H/F值（调试用）
                    if (showNodeGizmos) {
                        DrawNodeInfo(node, nodePosition);
                    }
                }
            }
        }

        // 获取节点应显示的颜色
        private Color GetNodeColor(Node node, int x, int y) {
            // 优先显示起点和终点
            if (debugStart != null && node == grid.NodeFromWorldPoint(debugStart.position)) {
                return startNodeColor;
            }
            if (debugTarget != null && node == grid.NodeFromWorldPoint(debugTarget.position)) {
                return targetNodeColor;
            }

            // 根据通行状态显示颜色
            if (!node.walkable && showUnwalkable) {
                return unwalkableColor;
            }
            if (node.walkable && showWalkable) {
                return walkableColor;
            }

            return Color.clear;
        }

        // 绘制节点的G/H/F值（在Scene视图中显示文本）
        private void DrawNodeInfo(Node node, Vector3 position) {
            // 仅在Scene视图且选中该物体时显示文本
            if (!Application.isPlaying || !Selection.Contains(gameObject)) return;

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.black;
            style.fontSize = 10;
            
            // 显示F值（G+H）
            string text = $"F: {node.fCost}\nG: {node.gCost}\nH: {node.hCost}";
            UnityEditor.Handles.Label(position + Vector3.up * 0.1f, text, style);
        }
    }
}