using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;

namespace App.Combat
{
    [BurstCompile]
    public partial struct SpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 严格等待：只有当场景里存在 SpawnerConfig 图纸时，系统才允许启动
            state.RequireForUpdate<SpawnerConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 绝对防线：暴兵只执行一帧，随后彻底沉睡，零 CPU 占用
            state.Enabled = false;

            var spawner = SystemAPI.GetSingleton<SpawnerConfig>();
            
            // 使用临时内存池创建信箱，保证执行完瞬间回收，没有任何 GC 垃圾
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // 引入随机数种子（实战中可以用系统时间戳 uint 作为种子）
            var random = new Unity.Mathematics.Random(8888); 

            // 申请连续的内存阵列，瞬间完成万级实体的克隆
            var spawnedEntities = new NativeArray<Entity>(spawner.SpawnCount, Allocator.Temp);
            state.EntityManager.Instantiate(spawner.MonsterPrefab, spawnedEntities);

            // 遍历并赋予初始空间位置
            for (int i = 0; i < spawnedEntities.Length; i++)
            {
                var entity = spawnedEntities[i];
                
                // 环形生成逻辑：让怪物在距离中心 15 米到 30 米的环带内出现，避免骑脸生成
                float2 randomDir = random.NextFloat2Direction();
                float randomDist = random.NextFloat(15f, 30f); 
                float3 spawnPos = new float3(randomDir.x * randomDist, 0, randomDir.y * randomDist);

                // 设定初始位置，并让怪物全部默认面向地图中心 (0,0,0)
                var initialTransform = LocalTransform.FromPositionRotation(
                    spawnPos, 
                    quaternion.LookRotationSafe(-spawnPos, math.up())
                );

                ecb.SetComponent(entity, initialTransform);
            }

            // 提交信箱，让修改在内存中正式生效
            ecb.Playback(state.EntityManager);
            
            // 释放临时内存
            spawnedEntities.Dispose();
            ecb.Dispose();
        }
    }
}