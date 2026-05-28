using Unity.Entities;
using UnityEngine;

namespace App.Combat
{
    // 最小运行反馈：直接在屏幕角落显示玩家经验与等级
    // 这样可以快速确认“击杀 -> 掉落 -> 吸收 -> 经验增长”这条闭环是否真的成立
    public class CombatDebugHUD : MonoBehaviour
    {
        [Header("显示开关")]
        [Tooltip("是否显示调试 HUD。")]
        public bool Enabled = true;

        private bool _worldReady;
        private bool _hasPlayer;
        private int _playerExp;
        private int _playerLevel;
        private string _statusText = "[CombatDebug] Waiting for ECS world...";

        private void Update()
        {
            if (!Enabled)
            {
                return;
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                _worldReady = false;
                _hasPlayer = false;
                _statusText = "[CombatDebug] ECS world not ready";
                return;
            }

            _worldReady = true;
            RefreshPlayerState(world.EntityManager);
        }

        private void OnGUI()
        {
            if (!Enabled)
            {
                return;
            }

            GUI.Label(new Rect(12, 12, 520, 24), _statusText);

            if (!_worldReady)
            {
                return;
            }

            if (!_hasPlayer)
            {
                GUI.Label(new Rect(12, 36, 520, 24), "[CombatDebug] PlayerExp not found");
                return;
            }

            GUI.Label(new Rect(12, 36, 520, 24), $"[CombatDebug] Level: {_playerLevel}  Exp: {_playerExp}");
        }

        private void RefreshPlayerState(EntityManager entityManager)
        {
            try
            {
                using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<PlayerExp>());
                if (query.IsEmptyIgnoreFilter)
                {
                    _hasPlayer = false;
                    _statusText = "[CombatDebug] PlayerExp not found";
                    return;
                }

                var entity = query.GetSingletonEntity();
                var playerExp = entityManager.GetComponentData<PlayerExp>(entity);
                _playerExp = playerExp.CurrentExp;
                _playerLevel = playerExp.Level;
                _hasPlayer = true;
                _statusText = "[CombatDebug] Player data refreshed";
            }
            catch (System.Exception ex)
            {
                _hasPlayer = false;
                _statusText = $"[CombatDebug] Refresh failed: {ex.Message}";
            }
        }
    }
}
