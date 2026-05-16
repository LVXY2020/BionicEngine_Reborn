using Unity.Entities;
using UnityEngine;

namespace App.Combat
{
    // 挂在你的怪物预制体 (Monster Prefab) 上
    public class MonsterAuthoring : MonoBehaviour
    {
        [Header("基础属性")]
        public float MaxHealth = 100f;

        [Header("仿生机能 (Bionic)")]
        public float MaxSpeed = 3f;
        public float Acceleration = 10f;
        public float Mass = 1f;       // 普通小怪填 1，如果是精英怪可以填 10
        public float Radius = 0.5f;   // 物理碰撞半径

        public class MonsterBaker : Baker<MonsterAuthoring>
        {
            public override void Bake(MonsterAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                // 1. 注入身份与生命基因
                AddComponent(entity, new MonsterTag());
                AddComponent(entity, new Health 
                { 
                    Current = authoring.MaxHealth, 
                    Max = authoring.MaxHealth 
                });

                // 🌟 2. 注入自研仿生四件套 (完全平替 ProjectDawn)
                AddComponent(entity, new BionicLocomotion 
                { 
                    MaxSpeed = authoring.MaxSpeed, 
                    Acceleration = authoring.Acceleration, 
                    Mass = authoring.Mass 
                });
                AddComponent(entity, new BionicShape { Radius = authoring.Radius });
                
                // 大脑意图和当前物理速度，初始全为 0，由 System 每帧去计算
                AddComponent(entity, new BionicSteering()); 
                AddComponent(entity, new BionicBody()); 

             
                // 在你的 Baker 类的 Bake 方法中添加：
                AddComponent(entity, new MonsterAttackStats 
                { 
                Damage = 10f, 
                AttackRangeSq = 1.5f * 1.5f, // 假设攻击距离是1.5米
                AttackCooldown = 1.0f        // 每1秒咬一次
                });
                AddComponent(entity, new AttackTimer { Value = 0f });    

                // 3. 动画接管预留 (Latios Kinemation)
                // 只要预制体上有 Animator，Kinemation 会自动接管，这里无需手动写代码。
            }
        }
    }
}