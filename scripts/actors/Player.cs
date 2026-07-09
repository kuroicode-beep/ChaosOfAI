// scripts/actors/Player.cs
// 격투가 플레이어. M0(클릭 이동) + M1(근접 공격 파이프라인) 뼈대.
// 씬 구성 기대치(자식 노드): NavigationAgent3D, MeleeHitbox, (AnimationPlayer는 아트 단계에서 추가).
// Sonnet 확장 지점: 애니메이션 연동, 스킬 3종 분기, 스탯/레벨업 UI 반영.

using System;
using Godot;
using ChaosOfAI.Combat;
using ChaosOfAI.Resources;
using ChaosOfAI.UI;

namespace ChaosOfAI.Actors
{
    public partial class Player : CharacterBody3D, IDamageable
    {
        [Export] public float MoveSpeed = 5.0f;
        [Export] public SkillData? StrikeSkill;   // data/skills/strike.tres
        [Export] public SkillData? CrushSkill;    // data/skills/crush.tres
        [Export] public SkillData? SpinSkill;     // data/skills/spin.tres
        [Export] public NodePath CameraPath = new();

        // Godot 노드 참조는 _Ready()에서 GetNode로 확정 초기화됨(null! 관용구).
        private NavigationAgent3D _nav = null!;
        private MeleeHitbox _hitbox = null!;
        private Camera3D? _camera;
        private readonly Random _rng = new();

        private CombatStats _stats = null!;
        private PlayerProgression _progression = null!;

        // 공격 상태 머신(간이)
        private SkillData? _activeSkill;
        private float _attackTimer;
        private bool _windowOpen;

        // M4(간소화): 그리드/장착 UI 없이 습득 아이템 목록만 보유, 보너스는 즉시 적용(§ 아키텍처 노트).
        public readonly System.Collections.Generic.List<ItemData> Inventory = new();

        public CombatStats Stats => _stats;
        public PlayerProgression Progression => _progression;
        public bool IsAlive => _stats?.IsAlive ?? true;

        public override void _Ready()
        {
            AddToGroup("player"); // EnemyAI가 그룹으로 탐지(§ M2)
            _nav = GetNode<NavigationAgent3D>("NavigationAgent3D");
            _hitbox = GetNode<MeleeHitbox>("MeleeHitbox");
            if (CameraPath != null && !CameraPath.IsEmpty)
                _camera = GetNode<Camera3D>(CameraPath);

            // 격투가 초기 스탯: STR/VIT 중심(§3)
            _stats = new CombatStats(1, new PrimaryAttributes(str: 25, dex: 15, vit: 20, ene: 10));
            _progression = new PlayerProgression(_stats);

            // 스킬 .tres 미할당 시 코드 폴백(SkillLibrary)으로 M1 검증 가능하게
            StrikeSkill ??= SkillLibrary.Strike();
            CrushSkill ??= SkillLibrary.Crush();
            SpinSkill ??= SkillLibrary.Spin();

            if (_camera != null)
                CombatFeedback.Instance?.RegisterCamera(_camera);
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (Input.IsActionJustPressed("click_move"))
                MoveToMouse();
            else if (Input.IsActionJustPressed("skill_strike"))
                BeginAttack(StrikeSkill);
            else if (Input.IsActionJustPressed("skill_crush"))
                BeginAttack(CrushSkill);
            else if (Input.IsActionJustPressed("skill_spin"))
                BeginAttack(SpinSkill);
            else if (Input.IsActionJustPressed("alloc_str"))
                _progression.SpendStatPoint(StatKind.Strength);
            else if (Input.IsActionJustPressed("alloc_dex"))
                _progression.SpendStatPoint(StatKind.Dexterity);
            else if (Input.IsActionJustPressed("alloc_vit"))
                _progression.SpendStatPoint(StatKind.Vitality);
            else if (Input.IsActionJustPressed("alloc_ene"))
                _progression.SpendStatPoint(StatKind.Energy);
        }

        /// <summary>아이템 습득(M4 간소화): 목록에 추가 + 접사를 CombatStats.Equipment에 즉시 합산.</summary>
        public void PickupItem(ItemData item)
        {
            Inventory.Add(item);
            var eq = _stats.Equipment;
            eq.BonusStrength += item.BonusStrength;
            eq.FlatDefense += item.BonusDefense;
            eq.FlatAttackRating += item.BonusAttackRating;
            eq.FlatMaxHp += item.BonusMaxHp;
            eq.FlatMinDamage += item.BonusMinDamage;
            eq.FlatMaxDamage += item.BonusMaxDamage;
            _stats.Equipment = eq;
        }

        // 화면 클릭 → 지면(y=0 평면) 교차점으로 이동 목표 설정(M0).
        private void MoveToMouse()
        {
            if (_camera == null) return;
            Vector2 mouse = GetViewport().GetMousePosition();
            Vector3 from = _camera.ProjectRayOrigin(mouse);
            Vector3 dir = _camera.ProjectRayNormal(mouse);
            if (Mathf.IsZeroApprox(dir.Y)) return;
            float t = -from.Y / dir.Y; // 지면 평면 y=0 교차
            if (t < 0) return;
            Vector3 target = from + dir * t;
            _nav.TargetPosition = target;
        }

        public override void _PhysicsProcess(double delta)
        {
            HandleAttack(delta);
            HandleMovement();
            SyncHud();
        }

        private void HandleMovement()
        {
            // 공격 중(윈도 열림)엔 이동 정지 → 헛방 방지
            if (_windowOpen) { Velocity = Vector3.Zero; MoveAndSlide(); return; }

            if (_nav.IsNavigationFinished()) { Velocity = Vector3.Zero; MoveAndSlide(); return; }

            Vector3 next = _nav.GetNextPathPosition();
            Vector3 dir = (next - GlobalPosition);
            dir.Y = 0;
            if (dir.LengthSquared() > 0.0001f)
            {
                dir = dir.Normalized();
                Velocity = dir * MoveSpeed;
                FaceDirection(dir);
            }
            MoveAndSlide();
        }

        private void BeginAttack(SkillData? skill)
        {
            if (skill == null || _activeSkill != null) return; // 이미 공격 중이면 무시(간이)
            if (!_stats.SpendMp(skill.ManaCost)) return;

            _activeSkill = skill;
            _attackTimer = 0f;
            _windowOpen = false;

            // 마우스 방향으로 회전(타격 방향 확정)
            FaceMouse();

            // 히트박스 형상 오버라이드 적용
            if (skill.RangeOverride > 0) _hitbox.Range = skill.RangeOverride;
            if (skill.ConeHalfAngleOverride > 0) _hitbox.ConeHalfAngleDeg = skill.ConeHalfAngleOverride;
        }

        private void HandleAttack(double delta)
        {
            if (_activeSkill == null) return;
            _attackTimer += (float)delta;

            // 액티브 윈도 진입
            if (!_windowOpen && _attackTimer >= _activeSkill.ActiveWindowStart)
            {
                _windowOpen = true;
                _hitbox.BeginSwing();
            }

            // 윈도 동안 매 물리프레임 Strike(캐시로 다단히트 방지, spin류는 회전하며 여러 대상)
            if (_windowOpen && _attackTimer <= _activeSkill.ActiveWindowEnd)
            {
                var hits = _hitbox.Strike(BuildAttackProfile(_activeSkill), this,
                    _activeSkill.KnockbackScale, _rng);
                if (hits.Count > 0) TriggerFeedback(hits, _activeSkill);
            }

            // 윈도 종료
            if (_windowOpen && _attackTimer > _activeSkill.ActiveWindowEnd)
            {
                _hitbox.EndSwing();
                _windowOpen = false;
            }

            // 스킬 종료(윈도 끝 + 약간의 후딜)
            if (_attackTimer > _activeSkill.ActiveWindowEnd + _activeSkill.Cooldown)
                _activeSkill = null;
        }

        private AttackProfile BuildAttackProfile(SkillData skill)
        {
            // 주먹 기본 데미지 + 장비 접사. MVP 초기값.
            int min = 3 + _stats.Equipment.FlatMinDamage;
            int max = 7 + _stats.Equipment.FlatMaxDamage;
            return new AttackProfile(
                minDamage: min,
                maxDamage: max,
                strength: _stats.EffectiveStrength,
                skillMultiplier: skill.DamageMultiplier,
                attackRating: _stats.AttackRating,
                level: _stats.Level,
                critChance: BalanceConstants.BaseCritChance);
        }

        private void TriggerFeedback(System.Collections.Generic.List<HitApplication> hits, SkillData skill)
        {
            bool anyHit = false, anyKill = false;
            foreach (var h in hits)
            {
                if (h.Result.Hit) anyHit = true;
                if (!h.Target.IsAlive) anyKill = true;
            }
            if (anyHit)
            {
                CombatFeedback.Instance?.HitStop(skill.HeavyHitStop || anyKill);
                CombatFeedback.Instance?.Shake(anyKill ? 0.12f : 0.05f);
            }
        }

        // ── 회전 유틸 ─────────────────────────────────────
        private void FaceMouse()
        {
            if (_camera == null) return;
            Vector2 mouse = GetViewport().GetMousePosition();
            Vector3 from = _camera.ProjectRayOrigin(mouse);
            Vector3 dir = _camera.ProjectRayNormal(mouse);
            if (Mathf.IsZeroApprox(dir.Y)) return;
            float t = -from.Y / dir.Y;
            if (t < 0) return;
            Vector3 target = from + dir * t;
            Vector3 face = target - GlobalPosition; face.Y = 0;
            if (face.LengthSquared() > 0.0001f) FaceDirection(face.Normalized());
        }

        private void FaceDirection(Vector3 dir)
        {
            float yaw = Mathf.Atan2(dir.X, dir.Z);
            Rotation = new Vector3(0, yaw, 0);
        }

        // ── IDamageable ──────────────────────────────────
        public DefenderProfile GetDefenderProfile()
            => new DefenderProfile(_stats.Defense, 0f, _stats.Level);

        public void ReceiveHit(in DamageResult result, Vector3 knockbackDir, float knockbackStrength)
        {
            if (!result.Hit)
            {
                DamageNumberSpawner.Instance?.Spawn(GlobalPosition, 0f, false, miss: true);
                return;
            }
            _stats.ApplyDamage(result.Amount);
            DamageNumberSpawner.Instance?.Spawn(GlobalPosition, result.Amount, result.IsCritical, miss: false);
            // TODO(Sonnet): 피격 이펙트/사운드, 사망 시 게임오버 화면(아트 자산 필요).
        }

        // ── HUD 연동 ─────────────────────────────────────
        private Hud? _hud;

        private void SyncHud()
        {
            _hud ??= GetTree().GetFirstNodeInGroup("hud") as Hud;
            _hud?.UpdateVitals(_stats.CurrentHp, _stats.MaxHp, _stats.CurrentMp, _stats.MaxMp);
            _hud?.UpdateProgression(_stats.Level, _progression.CurrentXp, _progression.XpToNextLevel,
                _progression.UnspentStatPoints, _progression.UnspentSkillPoints);
        }
    }
}
