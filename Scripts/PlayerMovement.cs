using System.Collections.Generic;
using UnityEngine;

namespace A_Star.Scripts
{
    [RequireComponent(typeof(Pathfinding))]
    public class PlayerMovement : MonoBehaviour
    {
        public Transform target; // 目标点
        public float moveSpeed = 5f; // 移动速度
        public float rotationSpeed = 10f; // 转向速度
        public float waypointDistance = 0.5f; // 到达路点的判定距离

        private Pathfinding pathfinding;
        private List<Node> currentPath; // 当前路径
        private int currentWaypointIndex; // 当前路点索引
        
        private Vector3 lastTargetPosition; // 记录上一帧目标位置
        private float lastPathCheckTime; // 上一次路径检查时间
        public float targetMoveThreshold = 0.1f; // 目标移动超过这个距离才重新计算路径
        public float pathCheckInterval = 0.2f; // 路径有效性检查间隔（秒）

        void Start()
        {
            pathfinding = GetComponent<Pathfinding>();
            currentWaypointIndex = 0;
            
            // 初始化目标位置
            if (target != null)
                lastTargetPosition = target.position;
        }

        void Update()
        {
            // 定期检查路径是否有效或目标是否移动
            if (Time.time - lastPathCheckTime > pathCheckInterval)
            {
                CheckAndUpdatePath();
                lastPathCheckTime = Time.time;
            }

            // 移动到目标
            MoveAlongPath();
        }

        // 检查并更新路径（只在必要时）
        void CheckAndUpdatePath()
        {
            if (target is null) return;

            // 情况1：目标移动超过阈值，需要重新计算路径
            bool targetMoved = Vector3.Distance(target.position, lastTargetPosition) > targetMoveThreshold;
            
            // 情况2：当前路径无效（存在不可走的节点）
            bool pathInvalid = IsPathInvalid();
            
            // 只有在上述两种情况之一时，才重新计算路径
            if (targetMoved || pathInvalid || currentPath == null)
            {
                currentPath = pathfinding.FindPath(transform.position, target.position);
                currentWaypointIndex = 0;
                lastTargetPosition = target.position; // 更新目标位置记录
            }
        }

        // 检查当前路径是否无效（包含不可通行的节点）
        bool IsPathInvalid()
        {
            if (currentPath == null || currentPath.Count == 0)
                return false;

            foreach (Node node in currentPath)
            {
                if (!node.walkable || !node.climbable)
                    return true; // 发现不可走的节点，路径无效
            }
            return false;
        }

        // 沿路径移动
        void MoveAlongPath()
        {
            // 如果没有路径或已到达终点，停止移动
            if (currentPath == null || currentWaypointIndex >= currentPath.Count)
                return;

            // 获取当前目标路点
            Node targetWaypoint = currentPath[currentWaypointIndex];
            Vector3 targetPosition = targetWaypoint.worldPosition;

            // 移动到路点
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            // 转向目标方向
            if (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetPosition - transform.position);
                transform.rotation = Quaternion.Lerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }

            // 检查是否到达当前路点
            if (Vector3.Distance(transform.position, targetPosition) < waypointDistance)
            {
                currentWaypointIndex++;
            }
        }

        // 调试用：在Scene视图中绘制路径
        void OnDrawGizmos()
        {
            if (currentPath != null)
            {
                Gizmos.color = Color.green;
                // 绘制Player到第一个路点的线
                // if (currentPath.Count > 0)
                // {
                //     Gizmos.DrawLine(transform.position, currentPath[0].worldPosition);
                // }

                // 绘制路点之间的线
                for (int i = 0; i < currentPath.Count - 1; i++)
                {
                    Gizmos.DrawLine(
                        currentPath[i].worldPosition,
                        currentPath[i + 1].worldPosition
                    );
                }
            }
        }
    }
}
