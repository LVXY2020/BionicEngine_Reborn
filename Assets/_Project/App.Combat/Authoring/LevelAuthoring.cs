using Unity.Entities;
using UnityEngine;

namespace App.Combat
{
    // 建议挂在场景中的一个全局配置空物体上
    public class LevelAuthoring : MonoBehaviour
    {
        [Header("战利品配置")]
        public GameObject ExpGemPrefab; // 拖入你的经验宝石预制体

        public class LevelBaker : Baker<LevelAuthoring>
        {
            public override void Bake(LevelAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                
                // 🌟 将 GameObject 预制体转换为 ECS 内部的 Entity 蓝图
                var gemEntity = GetEntity(authoring.ExpGemPrefab, TransformUsageFlags.Dynamic);

                AddComponent(entity, new GlobalLevelConfig
                {
                    ExpGemPrefab = gemEntity
                });
            }
        }
    }
}