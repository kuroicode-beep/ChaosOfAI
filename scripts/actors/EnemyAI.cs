// scripts/actors/EnemyAI.cs
// 고장난 AI(몬스터). M1에서는 "피격 가능한 표적"으로 먼저 쓰이고, M2에서 추격→공격 AI를 채운다.
// 씬 기대치(자식): NavigationAgent3D (M2). EnemyData(.tres)로 스탯 주입.
// Sonnet 확장 지점: 상태머신(Idle/Chase/Attack/Dead), 애니메이션, 드랍/경험치.

using Godot;
using ChaosOfAI.Combat;
using ChaosOfAI.Resources;

namespace ChaosOfAI.Actors
{
    public enum EnemyState { Idle, Chase, Attack, Dead }

    public partial class EnemyAI : CharacterBody3D, IDamageable
    {
        [Export] public EnemyData Data;

        [Signal] public delegate void DiedEventHandler(); // 드랍/경험치 훅

        private CombatStats _stats;
        public EnemyState State { get; private set; } = EnemyState.Idle;

        public bool IsAlive => _stats?.IsAlive ?? false;

        public override void _Ready()
        {
            if (Data == null)
            {
                GD.PushWarning("EnemyAI: EnemyData 미할당 — 기본값으로 대체.");
                Data = new EnemyData();
            }
            _stats = Data.CreateStats();
        }

        // ── IDamageable ──────────────────────────────────
        public DefenderProfile GetDefenderProfile()
            => Data.CreateDefenderProfile(_stats);

        public void ReceiveHit(in DamageResult result, Vector3 knockbackDir, float knockbackStrength)
        {
            if (State == EnemyState.Dead) return;

            if (!result.Hit)
            {
                // TODO(Sonnet): "MISS" 데미지 넘버 표시.
                return;
            }

            _stats.ApplyDamage(result.Amount);

            // 넉백(간이): 즉시 위치 밀기. Sonnet이 물리 기반으로 개선 가능.
            if (knockbackStrength > 0f)
                GlobalPosition += knockbackDir * knockbackStrength * 0.1f;

            // TODO(Sonnet): flinch 애니메이션 + 피격 발광, 데미지 넘버(result.Amount, result.IsCritical).

            if (!_stats.IsAlive)
                Die();
        }

        private void Die()
        {
            State = EnemyState.Dead;
            EmitSignal(SignalName.Died);
            // TODO(Sonnet): 사망 연출 후 드랍(Data.DropChance)/경험치(Data.XpReward), QueueFree.
            QueueFree();
        }

        // ── AI 골격(M2에서 구현) ───────────────────────────
        public override void _PhysicsProcess(double delta)
        {
            // TODO(Sonnet M2): 플레이어 탐지(DetectionRange) → Chase → AttackRange 도달 시 Attack.
        }
    }
}
