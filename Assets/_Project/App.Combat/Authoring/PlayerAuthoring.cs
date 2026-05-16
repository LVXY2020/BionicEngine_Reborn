using Unity.Entities;
using UnityEngine;

namespace App.Combat
{
    // 挂在你的主角预制体 (Player Prefab) 上
    public class PlayerAuthoring : MonoBehaviour
    {
        [Header("基础移动")]
        public float MoveSpeed = 6f;
        public float Radius = 0.6f;
        
        [Header("仿生物理")]
        public float Mass = 1000f; // 🌟 补上这行！把它暴露给 Unity 面板

        public class PlayerBaker : Unity.Entities.Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                // 1. 注入玩家专属基因
                AddComponent(entity, new PlayerTag());
                AddComponent(entity, new PlayerInput()); 
                AddComponent(entity, new PlayerExp { CurrentExp = 0, Level = 1 });

                // 2. 注入自研仿生雷达基因
                AddComponent(entity, new BionicShape { Radius = authoring.Radius });
                AddComponent(entity, new BionicBody { Velocity = Unity.Mathematics.float3.zero });
                
                // 🌟 赋予主角极其恐怖的质量
                AddComponent(entity, new BionicLocomotion 
                { 
                    MaxSpeed = authoring.MoveSpeed, 
                    Acceleration = 50f, 
                    Mass = authoring.Mass // 🌟 这里改为读取 authoring 面板上的数值，不再写死
                });
            }
        }
    }
}