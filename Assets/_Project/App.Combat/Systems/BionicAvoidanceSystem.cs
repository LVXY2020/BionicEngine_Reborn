using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Collections;

namespace App.Combat
{
    // 🌟 核心防线：彻底断绝 Unity  vanilla 默认世界的盲目自动实例化
    [DisableAutoCreation]
    [BurstCompile]
    public partial struct BionicAvoidanceSystem : ISystem
    {
        // 声明组件雷达，用于在多线程中安全偷窥其他实体的数据
        private ComponentLookup<BionicBody> _bodyLookup;
        private ComponentLookup<BionicShape> _shapeLookup;
        private ComponentLookup<BionicLocomotion> _locoLookup;
        private ComponentLookup<WorldTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 只有场上存在 BionicSteering 的实体时，避障系统才需要工作
            state.RequireForUpdate<BionicSteering>();

            _bodyLookup = state.GetComponentLookup<BionicBody>(true);
            _shapeLookup = state.GetComponentLookup<BionicShape>(true);
            _locoLookup = state.GetComponentLookup<BionicLocomotion>(true);
            _transformLookup = state.GetComponentLookup<WorldTransform>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 获取上一道工序建好的雷达网
            var layerEntity = SystemAPI.GetSingletonEntity<BionicCollisionLayerSystem.Singleton>();
            var layer = SystemAPI.GetComponent<BionicCollisionLayerSystem.Singleton>(layerEntity).Layer;

            // 每帧必须更新雷达指针
            _bodyLookup.Update(ref state);
            _shapeLookup.Update(ref state);
            _locoLookup.Update(ref state);
            _transformLookup.Update(ref state);

            // 先根据目标方向生成初始意图，再根据附近实体做软排斥修正
            new AvoidanceJob
            {
                Layer = layer,
                BodyLookup = _bodyLookup,
                ShapeLookup = _shapeLookup,
                LocoLookup = _locoLookup,
                TransformLookup = _transformLookup
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    // 只有长了脑子 (Steering) 的实体才参与计算，主角(无Steering)自动豁免！
    public partial struct AvoidanceJob : IJobEntity
    {
        [ReadOnly] public CollisionLayer Layer;
        [ReadOnly] public ComponentLookup<BionicBody> BodyLookup;
        [ReadOnly] public ComponentLookup<BionicShape> ShapeLookup;
        [ReadOnly] public ComponentLookup<BionicLocomotion> LocoLookup;
        [ReadOnly] public ComponentLookup<WorldTransform> TransformLookup;

        public void Execute(Entity entity, ref BionicSteering steering, in BionicBody body, in WorldTransform transform, in BionicShape shape, in BionicLocomotion loco)
        {
            // 1. 计算初始驱动力：不顾一切冲向目标的速度向量
            float3 toTarget = math.normalizesafe(steering.TargetPosition - transform.position);
            float3 desiredVel = toTarget * loco.MaxSpeed;

            // 2. 环境感知设定
            float searchRadius = shape.Radius * 3.0f; // 扫描周围 3 倍半径的邻居

            Aabb searchAabb = new Aabb(transform.position - searchRadius, transform.position + searchRadius);

            var processor = new SeparationProcessor
            {
                SelfEntity = entity,
                SelfPos = transform.position,
                SelfVel = body.Velocity,
                SelfRadius = shape.Radius,
                SelfMass = loco.Mass,
                Force = float3.zero, // 🌟 修复：以普通字段初始化为零向量
                BodyLookup = BodyLookup,
                ShapeLookup = ShapeLookup,
                LocoLookup = LocoLookup,
                TransformLookup = TransformLookup
            };

            // 3. 执行空间查询 (Latios底层会将 processor 作为引用传递，内部的累加力会被保存)
            Physics.FindObjects(searchAabb, Layer, processor);

            // 4. 融合最终意图 (安全读取 processor.Force 算完后的排斥力)
            steering.DesiredVelocity = desiredVel + processor.Force; 
        }
    }

    public struct SeparationProcessor : IFindObjectsProcessor
    {
        public Entity SelfEntity;
        public float3 SelfPos;
        public float3 SelfVel;
        public float SelfRadius;
        public float SelfMass;
        public float3 Force; // 🌟 修复：移除 ref 关键字，降级为普通结构体字段

        // 雷达引用
        public ComponentLookup<BionicBody> BodyLookup;
        public ComponentLookup<BionicShape> ShapeLookup;
        public ComponentLookup<BionicLocomotion> LocoLookup;
        public ComponentLookup<WorldTransform> TransformLookup;

        public void Execute(in FindObjectsResult result)
        {
            Entity neighbor = result.entity;
            
            // 绝对防线：不能自己推自己
            if (neighbor == SelfEntity) return;

            float3 neighborPos = TransformLookup[neighbor].position;
            float neighborRadius = ShapeLookup[neighbor].Radius;

            float3 diff = SelfPos - neighborPos;
            float dist = math.length(diff);
            float minSafeDist = SelfRadius + neighborRadius;

            // 如果发生了拥挤重叠
            if (dist < minSafeDist && dist > 0.001f)
            {
                float3 neighborVel = BodyLookup[neighbor].Velocity;
                float neighborMass = LocoLookup[neighbor].Mass;

                // 核心科技 1：RVO 同向免责。大家都在往同方向冲，就不要互相推挤 (消除鬼畜)
                float alignment = math.dot(math.normalizesafe(SelfVel), math.normalizesafe(neighborVel));
                float forceWeight = 1.0f - math.saturate(alignment * 0.8f);

                // 核心科技 2：软排斥。挤得越深，推力越大，但有指数上限
                float pushStrength = math.min(3.0f, (minSafeDist - dist) / minSafeDist);
                float3 pushDir = diff / dist;
                
                // 核心科技 3：质量霸权。遇到主角 (Mass=1000) 时，怪物被瞬间弹开
                float massRatio = neighborMass / SelfMass;

                // 累加排斥力
                Force += pushDir * pushStrength * forceWeight * massRatio * 8f;
            }
        }
    }
}