using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Latios;

namespace App.Combat
{
    // 强制在战斗和掉落结算之后执行
    // 🌟 核心防线：彻底断绝 Unity  vanilla 默认世界的盲目自动实例化
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DamageCollisionSystem))]
    [BurstCompile]
    public partial struct GemMagnetSystem : ISystem
    {
        private ComponentLookup<PlayerExp> _expLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 开启读写权限，准备给主角加经验
            _expLookup = state.GetComponentLookup<PlayerExp>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<PlayerTag>(out var playerEntity)) return;
            var playerPos = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;

            _expLookup.Update(ref state);

            // 使用 Latios 帧末信箱处理销毁，绝不产生内存碎片
            var latiosWorld = state.GetLatiosWorldUnmanaged();
            var ecb = latiosWorld.syncPoint.CreateEntityCommandBuffer();

            new MagnetJob
            {
                PlayerPos = playerPos,
                PlayerEntity = playerEntity,
                DeltaTime = SystemAPI.Time.DeltaTime,
                MagnetRadiusSq = 25f, // 吸引半径的平方 (5米的平方)
                PickupRadiusSq = 0.5f, // 拾取半径的平方
                ExpLookup = _expLookup,
                ECB = ecb
            }.Schedule(); // 🌟 绝对防线：单线程排队执行，保证经验累加绝对准确，无脏数据！
        }
    }

    [BurstCompile]
    public partial struct MagnetJob : IJobEntity
    {
        public float3 PlayerPos;
        public Entity PlayerEntity;
        public float DeltaTime;
        public float MagnetRadiusSq;
        public float PickupRadiusSq;
        
        public ComponentLookup<PlayerExp> ExpLookup;
        public EntityCommandBuffer ECB;

        public void Execute(Entity gemEntity, ref ExpGem gem, ref LocalTransform transform)
        {
            float3 diff = PlayerPos - transform.Position;
            float distSq = math.lengthsq(diff);

            // 1. 引力捕获判定
            if (!gem.IsMagnetized && distSq < MagnetRadiusSq)
            {
                gem.IsMagnetized = true;
            }

            // 2. 引力牵引：一旦被捕获，无视距离，以 15m/s 的极速飞向主角
            if (gem.IsMagnetized)
            {
                float3 dir = math.normalizesafe(diff);
                transform.Position += dir * 15f * DeltaTime;

                // 3. 拾取与超度判定
                if (distSq < PickupRadiusSq)
                {
                    // 安全读取并累加经验
                    var playerExp = ExpLookup[PlayerEntity];
                    playerExp.CurrentExp += gem.ExpValue;
                    ExpLookup[PlayerEntity] = playerExp; // 写回内存

                    // 销毁宝石实体
                    ECB.DestroyEntity(gemEntity);
                }
            }
        }
    }
}