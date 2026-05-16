using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Latios.Psyshock; // 引入 Latios 物理核心
using Latios;
using Unity.Collections;

namespace App.Combat
{
    // 🌟 核心防线：彻底断绝 Unity  vanilla 默认世界的盲目自动实例化
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
            
            // 开启读写权限 (false 表示允许修改血量)
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
            // 1. 防穿透射击判定：根据速度还原上一帧的坐标，形成一条射线 (Ray)
            float3 endPos = transform.Position;
            float3 startPos = endPos - bullet.Velocity * DeltaTime;
            
            var ray = new Latios.Psyshock.Ray(startPos, endPos);

            // 🌟 修复 CS1501 & CS1061：传入 4 个参数，接收纯粹的数学命中 (hit) 和肉体信息 (hitInfo)
            if (Latios.Psyshock.Physics.Raycast(ray, Layer, out RaycastResult hit, out LayerBodyInfo hitInfo))
            {
                // 🌟 实体 ID 被存放在 LayerBodyInfo.body 里！
                Entity hitMonster = hitInfo.body.entity;

                // 安全校验：确认击中的是可以扣血的合法目标
                if (HealthLookup.HasComponent(hitMonster))
                {
                    var health = HealthLookup[hitMonster];

                    // 鞭尸阻断：如果怪物已经被上一颗子弹打死了，直接忽略
                    if (health.Current <= 0) return;

                    // 2. 真实扣血
                    health.Current -= bullet.Damage;
                    HealthLookup[hitMonster] = health; // 写回内存

                    // 3. 死亡与掉落结算
                    if (health.Current <= 0)
                    {
                        // 碾碎尸体
                        ECB.DestroyEntity(hitMonster);

                        // 爆出经验宝石
                        if (TransformLookup.HasComponent(hitMonster))
                        {
                            var monsterTransform = TransformLookup[hitMonster];
                            var gemEntity = ECB.Instantiate(GemPrefab);
                            ECB.SetComponent(gemEntity, monsterTransform);
                        }
                    }

                    // 无论死没死，子弹命中后自我销毁
                    ECB.DestroyEntity(bulletEntity);
                }
            }
            else
            {
                // 4. 内存清洁防线 (防 OOM 泄露)
                if (math.lengthsq(endPos) > 10000f) 
                {
                    ECB.DestroyEntity(bulletEntity);
                }
            }
        }
    }
}