using Unity.Entities;
using UnityEngine;

namespace App.Combat
{
    public class SpawnerAuthoring : MonoBehaviour
    {
        public GameObject MonsterPrefab; 
        public int SpawnCount = 1000;    

        public class SpawnerBaker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                // 刷怪器本身不需要移动，所以用 TransformUsageFlags.None
                var entity = GetEntity(TransformUsageFlags.None);
                
                // 将 Unity 面板里的 GameObject 压扁成 ECS 能懂的蓝图 Entity
                var monsterEntity = GetEntity(authoring.MonsterPrefab, TransformUsageFlags.Dynamic);
                
                // 🌟 核心闭环：把刷怪配置作为基因注入给实体！
                // 没有这一句，SpawnerSystem 绝对不会启动。
                AddComponent(entity, new SpawnerConfig 
                { 
                    MonsterPrefab = monsterEntity, 
                    SpawnCount = authoring.SpawnCount 
                });
            }
        }
    }
}