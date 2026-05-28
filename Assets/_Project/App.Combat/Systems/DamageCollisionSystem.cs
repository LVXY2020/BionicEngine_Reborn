using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Latios;
using Latios.Psyshock;

namespace App.Combat
{
    // 🌟 核心防线：彻底断绝 Unity vanilla 默认世界的盲目自动实例化
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BulletMovementSystem))]
    [BurstCompile]
    public partial struct DamageCollisionSystem : ISystem
    {
        private ComponentLookup<Health> _healthLookup;
        private ComponentLookup<LocalTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 严格等待：没有全局掉落图纸，法庭不予开庭
            state.RequireForUpdate<GlobalLevelConfig>();

            _healthLookup = state.GetComponentLookup<Health>(false);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 获取经验宝石蓝图
            var config = SystemAPI.GetSingleton<GlobalLevelConfig>();

            // 获取物理雷达网
            if (!SystemAPI.TryGetSingletonEntity<BionicCollisionLayerSystem.Singleton>(out var layerEntity)) return;
            var layer = SystemAPI.GetComponent<BionicCollisionLayerSystem.Singleton>(layerEntity).Layer;

            _healthLookup.Update(ref state);
            _transformLookup.Update(ref state);

            var latiosWorld = state.GetLatiosWorldUnmanaged();
            var ecb = latiosWorld.syncPoint.CreateEntityCommandBuffer();

            new CollisionJob
            {
                Layer = layer,
                DeltaTime = SystemAPI.Time.DeltaTime,
                GemPrefab = config.ExpGemPrefab,
                HealthLookup = _healthLookup,
                TransformLookup = _transformLookup,
                ECB = ecb
            }.Schedule();
        }
    }

    [BurstCompile]
    public partial struct CollisionJob : IJobEntity
    {
        [ReadOnly] public CollisionLayer Layer;
        public float DeltaTime;
        public Entity GemPrefab;

        public ComponentLookup<Health> HealthLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        public EntityCommandBuffer ECB;

        public void Execute(Entity bulletEntity, in LocalTransform transform, in BulletMovement bullet)
        {
            float3 endPos = transform.Position;
            float3 startPos = endPos - bullet.Velocity * DeltaTime;

            var ray = new Latios.Psyshock.Ray(startPos, endPos);

            if (Latios.Psyshock.Physics.Raycast(ray, Layer, out RaycastResult hit, out LayerBodyInfo hitInfo))
            {
                Entity hitMonster = hitInfo.body.entity;

                if (HealthLookup.HasComponent(hitMonster))
                {
                    var health = HealthLookup[hitMonster];

                    if (health.Current <= 0) return;

                    health.Current -= bullet.Damage;
                    HealthLookup[hitMonster] = health;

                    if (health.Current <= 0)
                    {
                        ECB.DestroyEntity(hitMonster);

                        if (TransformLookup.HasComponent(hitMonster))
                        {
                            var monsterTransform = TransformLookup[hitMonster];
                            var gemEntity = ECB.Instantiate(GemPrefab);
                            ECB.SetComponent(gemEntity, monsterTransform);
                        }
                    }

                    ECB.DestroyEntity(bulletEntity);
                }
            }
            else if (math.lengthsq(endPos) > 10000f)
            {
                ECB.DestroyEntity(bulletEntity);
            }
        }
    }
}
