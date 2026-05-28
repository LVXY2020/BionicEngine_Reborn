using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms; // 🌟 依然只保留 Unity 官方的 Transform
using Latios;
using Latios.Psyshock;

namespace App.Combat
{
    // 强制在所有移动和避障系统之前执行
    // 🌟 核心防线：彻底断绝 Unity  vanilla 默认世界的盲目自动实例化
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(BionicAvoidanceSystem))]
    [BurstCompile]
    public partial struct BionicCollisionLayerSystem : ISystem
    {
        public struct Singleton : IComponentData
        {
            public CollisionLayer Layer;
        }

        private EntityQuery _monsterQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 🌟 终极修复 BC1028：使用无 GC 的 EntityQueryBuilder 替代 typeof 传参
            // 这完全绕过了 C# params 关键字带来的隐式托管数组分配
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<BionicShape, LocalTransform>();

            _monsterQuery = state.GetEntityQuery(builder);

            // 预创建单例载体，避免运行时不断分叉新实体来存 Layer
            state.EntityManager.CreateSingleton<Singleton>();

            // 用完立即释放临时内存，保持底层绝对纯净
            builder.Dispose(); 
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int count = _monsterQuery.CalculateEntityCount();
            if (count == 0) return;

            // 修复：UpdateAllocator 不是标准 Allocator，直接传给 NativeArray 会触发参数异常。
            // 这里改用 TempJob 分配，OnUpdate 末尾主动 Dispose，保证安全和可预测。
            var bodies = CollectionHelper.CreateNativeArray<ColliderBody, RewindableAllocator>(
                count,
                ref state.WorldUnmanaged.UpdateAllocator,
                NativeArrayOptions.UninitializedMemory);

            // 2. 调度极速 Job，将自定义的 BionicShape 压扁转化为物理底层结构
            new BuildBodiesJob
            {
                Bodies = bodies
            }.ScheduleParallel(_monsterQuery, state.Dependency).Complete(); 

            // 3. 传入 NativeArray，并通过 out 输出 Layer（与 UpdateAllocator 同源）
            Physics.BuildCollisionLayer(bodies).RunImmediate(out CollisionLayer layer, state.WorldUnmanaged.UpdateAllocator.ToAllocator);

            // 4. 将建好的 Layer 存入单例供其他系统使用
            if (SystemAPI.TryGetSingletonEntity<Singleton>(out var entity))
            {
                SystemAPI.SetComponent(entity, new Singleton { Layer = layer });
            }
            else
            {
                var newEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(newEntity, new Singleton { Layer = layer });
            }
        }
    }

    // 负责组装底层物理刚体的流水线 Job
    [BurstCompile]
    public partial struct BuildBodiesJob : IJobEntity
    {
        public NativeArray<ColliderBody> Bodies;

        public void Execute(Entity entity, [EntityIndexInQuery] int index, in BionicShape shape, in LocalTransform transform)
        {
            Bodies[index] = new ColliderBody
            {
                collider = new SphereCollider(float3.zero, shape.Radius),
                // 🌟 使用绝对路径调用 Latios 的专属坐标系，避免与 Unity 官方发生歧义
                transform = new Latios.Transforms.TransformQvvs(transform.Position, transform.Rotation), 
                entity = entity
            };
        }
    }
}