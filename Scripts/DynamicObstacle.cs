using Unity.VisualScripting;
using UnityEngine;

namespace A_Star.Scripts
{
    public class DynamicObstacle: MonoBehaviour
    {
        public Grid grid => Grid.Singleton;
        public float updateRange = 1f; // 只更新障碍物周围1单位的网格
        
        
        // 障碍物激活/销毁时也需更新网格
        void OnEnable() => UpdateGridIfExists();
        void OnDisable() => UpdateGridIfExists();
        void UpdateGridIfExists()
        {
            if (grid != null)
                grid.UpdateGridArea(transform.position, updateRange);
        }
    }
}