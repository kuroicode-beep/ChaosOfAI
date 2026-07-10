// scripts/actors/EnemyAI.cs
// 고장난 AI(몬스터). M1 피격 표적 + M2 추격→공격 상태머신(Idle/Chase/Attack/Dead).
// 씬 기대치(자식): NavigationAgent3D, MeleeHitbox(공격 판정용), CollisionShape3D(본체, enemy 레이어).
// EnemyData(.tres)로 스탯 주입. Sonnet 확장 지점: 애니메이션, 드랍/경험치 실제 지급.

using System;
using Godot;
using ChaosOfAI.Combat;
using ChaosOfAI.Resources;

namespace ChaosOfAI.Actors
{
    public enum EnemyState { Idle, Chase, Attack, Dead }

    public partial class EnemyAI : CharacterBody3D, IDamageable
    {
        [Export] public EnemyData? Data;
        [Export] public NodePath? PlayerPath; // 미지정 시 그룹("player")에서 탐색

        [Signal] public delegate void DiedEventHandler(); // 드랍/경험치 훅

        private CombatStats _stats = null!;
        private EnemyData _data = null!; // Data 또는 기본값으로 _Ready에서 확정
        private MeleeHitbox? _hitbox;
        private Node3D? _player;
        private readonly Random _rng = new();

        private float _attackCooldownTimer;

        // 공격 액티브 윈도(간이): Area3D.Monitoring을 켠 직후 같은 물리 프레임에는
        // 겹침 목록이 아직 갱신되지 않는다(물리 서버가 다음 스텝에서 갱신) → 여러 프레임에
        // 걸쳐 Strike를 호출해야 실제로 명중 판정이 이뤄진다(Player.HandleAttack과 동일 패턴).
        private bool _attacking;
        private float _attackWindowTimer;

        public EnemyState State { get; private set; } = EnemyState.Idle;
        public bool IsAlive => _stats?.IsAlive ?? false;

        public override void _Ready()
        {
            _data = Data ?? new EnemyData();
            if (Data == null)
                GD.PushWarning($"EnemyAI({Name}): EnemyData 미할당 — 기본값으로 대체.");

            _stats = _data.CreateStats();
            _hitbox = GetNodeOrNull<MeleeHitbox>("MeleeHitbox");

            _player = ResolvePlayer();
        }

        private Node3D? ResolvePlayer()
        {
            if (PlayerPath != null && !PlayerPath.IsEmpty)
                return GetNodeOrNull<Node3D>(PlayerPath);
            var group = GetTree().GetFirstNodeInGroup("player");
            return group as Node3D;
        }

        // ── IDamageable ──────────────────────────────────
        public DefenderProfile GetDefenderProfile()
            => _data.CreateDefenderProfile(_stats);

        public void ReceiveHit(in DamageResult result, Vector3 knockbackDir, float knockbackStrength)
        {
            if (State == EnemyState.Dead) return;

            if (!result.Hit)
            {
                DamageNumberSpawner.Instance?.Spawn(GlobalPosition, 0f, false, miss: true);
                return;
            }

            _stats.ApplyDamage(result.Amount);
            DamageNumberSpawner.Instance?.Spawn(GlobalPosition, result.Amount, result.IsCritical, miss: false);

            // 넉백(간이): 즉시 위치 밀기. Sonnet이 물리 기반(velocity)으로 개선 가능.
            if (knockbackStrength > 0f)
                GlobalPosition += knockbackDir * knockbackStrength * 0.1f;

            // 피격 시 즉시 추격 전환(먼저 때리면 반응하도록)
            if (State == EnemyState.Idle) State = EnemyState.Chase;

            // TODO(Sonnet): flinch 애니메이션 + 피격 발광(아트 자산 필요).

            if (!_stats.IsAlive)
                Die();
        }

        private void Die()
        {
            State = EnemyState.Dead;
            EmitSignal(SignalName.Died);
            GrantRewardsToPlayer(); // M3/M4: 경험치 + 드랍(간소화, 즉시 장비 적용)
            // TODO(Sonnet): 사망 연출(아트 자산 필요).
            QueueFree();
        }

        private void GrantRewardsToPlayer()
        {
            if (_player is not Player player) return;

            player.Progression.GrantXp(_data.XpReward);

            var loot = LootTable.Roll(_data.DropChance, _rng);
            if (loot != null)
                player.PickupItem(loot);
        }

        // ── AI 상태머신 (M2) ────────────────────────────────
        // Idle: 대기, 탐지범위 안에 플레이어 진입 → Chase
        // Chase: NavigationAgent3D로 추격, 공격범위 도달 → Attack
        // Attack: 정지, 쿨다운마다 근접 공격(MeleeHitbox 1회 스윙), 범위 이탈 → Chase
        public override void _PhysicsProcess(double delta)
        {
            if (State == EnemyState.Dead) return;
            if (_player == null) { _player = ResolvePlayer(); if (_player == null) return; }

            float dt = (float)delta;
            if (_attackCooldownTimer > 0f) _attackCooldownTimer -= dt;

            Vector3 toPlayer = _player.GlobalPosition - GlobalPosition;
            toPlayer.Y = 0f;
            float dist = toPlayer.Length();

            switch (State)
            {
                case EnemyState.Idle:
                    Velocity = Vector3.Zero;
                    if (dist <= _data.DetectionRange) State = EnemyState.Chase;
                    MoveAndSlide();
                    break;

                case EnemyState.Chase:
                    if (dist <= _data.AttackRange)
                    {
                        State = EnemyState.Attack;
                        Velocity = Vector3.Zero;
                        MoveAndSlide();
                        break;
                    }
                    ChaseStep(dt);
                    break;

                case EnemyState.Attack:
                    Velocity = Vector3.Zero;
                    FaceDirection(dist > 0.01f ? toPlayer.Normalized() : Vector3.Forward);
                    if (!_attacking && dist > _data.AttackRange * 1.15f) // 약간의 여유(hysteresis)로 추격/공격 진동 방지
                    {
                        State = EnemyState.Chase;
                        break;
                    }
                    if (_attacking)
                        UpdateAttack(dt);
                    else if (_attackCooldownTimer <= 0f)
                        StartAttack();
                    MoveAndSlide();
                    break;
            }
        }

        // 평평한 무장애물 아레나이므로 플레이어를 향해 직선 추격(내비게이션은 M5 던전에서 도입).
        private void ChaseStep(float dt)
        {
            Vector3 dir = (_player!.GlobalPosition - GlobalPosition); dir.Y = 0;
            if (dir.LengthSquared() > 0.0001f)
            {
                dir = dir.Normalized();
                Velocity = dir * _data.MoveSpeed;
                FaceDirection(dir);
            }
            MoveAndSlide();
        }

        private void StartAttack()
        {
            _attackCooldownTimer = _data.AttackCooldown;
            if (_hitbox == null) return; // 히트박스 미배치면 접촉 판정 생략(스켈레톤 단계)

            _attacking = true;
            _attackWindowTimer = 0f;
            _hitbox.BeginSwing();
        }

        private void UpdateAttack(float dt)
        {
            _attackWindowTimer += dt;

            if (_attackWindowTimer <= BalanceConstants.EnemyAttackWindowSeconds)
            {
                var atk = new Combat.AttackProfile(
                    minDamage: _data.MinDamage,
                    maxDamage: _data.MaxDamage,
                    strength: _data.Strength,
                    skillMultiplier: 0f,
                    attackRating: _stats.AttackRating,
                    level: _stats.Level,
                    critChance: BalanceConstants.BaseCritChance);
                _hitbox!.Strike(atk, this, knockbackScale: 1f, _rng);
            }
            else
            {
                _hitbox!.EndSwing();
                _attacking = false;
            }
        }

        private void FaceDirection(Vector3 dir)
        {
            if (dir.LengthSquared() < 0.0001f) return;
            float yaw = Mathf.Atan2(dir.X, dir.Z);
            Rotation = new Vector3(0, yaw, 0);
        }
    }
}
