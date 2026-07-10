// scripts/actors/Player.cs
// 격투가 플레이어. M0(클릭 이동) + M1(근접 공격 파이프라인) + M3/M4(진행/장비) 통합.
// 씬 구성 기대치(자식 노드): NavigationAgent3D, MeleeHitbox, (AnimationPlayer는 아트 단계에서 추가).
// Sonnet 확장 지점: 애니메이션 연동, 스탯/레벨업 UI 시각화.

using System;
using System.Collections.Generic;
using Godot;
using ChaosOfAI.Combat;
using ChaosOfAI.Core;
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

        // 이동: 평평한 무장애물 아레나이므로 클릭 지점으로 직접 이동(내비게이션은 M5 던전에서 도입).
        // (내비 경로점이 navmesh 높이(y>0)로 반환돼 XZ 방향이 0이 되던 버그 회피)
        private Vector3 _moveTarget;
        private bool _hasMoveTarget;
        private const float ArriveDistance = 0.35f;

        private MeleeHitbox _hitbox = null!;
        private MeshInstance3D _swingVfx = null!;
        private Camera3D? _camera;
        private readonly Random _rng = new();

        // 효과음(합성 WAV). 외부 에셋 없이 손맛/입력 확인용.
        private AudioStream? _sfxSwing, _sfxCrush, _sfxHit, _sfxLevel;

        private CombatStats _stats = null!;
        private PlayerProgression _progression = null!;

        // 히트박스 씬 기본 형상(오버라이드 복원용) — 스킬이 override를 0으로 두면 이 값으로 되돌린다.
        private float _defaultRange;
        private float _defaultConeHalfAngle;

        // 공격 상태 머신(간이)
        private SkillData? _activeSkill;
        private float _attackTimer;
        private bool _windowOpen;
        private float _rehitTimer; // 다단타(RehitInterval) 누적 타이머

        private bool _dead;

        // 저장/불러오기는 export된 실행 파일에서만 자동 활성화된다(OS.HasFeature("standalone")).
        // 에디터/헤드리스 테스트 실행에서는 비활성 → 개발자의 스크린샷/회귀 테스트가 사용자의
        // 실제 플레이 세이브(user://save.json)를 절대 덮어쓰지 않는다.
        // 테스트가 저장 경로 자체를 검증하려면 이 플래그를 강제로 켜고 SaveSystem.FileName도 바꿀 것.
        public static bool ForceEnableSaveForTests = false;
        private bool _saveEnabled;

        // M4(간소화): 그리드/장착 UI 없이 습득 아이템 목록만 보유, 보너스는 즉시 적용(§ 아키텍처 노트).
        public readonly System.Collections.Generic.List<ItemData> Inventory = new();

        // 스킬 강화(§5.5 스킬 포인트 사용처): 스킬Id → 투자한 포인트 수(저장/복원용).
        private readonly Dictionary<string, int> _skillUpgrades = new();

        public CombatStats Stats => _stats;
        public PlayerProgression Progression => _progression;
        public bool IsAlive => !_dead && (_stats?.IsAlive ?? true);
        public bool IsAttacking => _activeSkill != null;      // 테스트/디버그: 스킬 발동 중인가
        public bool SwingVisible => _swingVfx?.Visible ?? false;

        public override void _Ready()
        {
            AddToGroup("player"); // EnemyAI가 그룹으로 탐지(§ M2)
            _hitbox = GetNode<MeleeHitbox>("MeleeHitbox");
            _swingVfx = GetNode<MeshInstance3D>("SwingVFX");
            _swingVfx.Visible = false;

            _defaultRange = _hitbox.Range;
            _defaultConeHalfAngle = _hitbox.ConeHalfAngleDeg;

            // 격투가 초기 스탯: STR/VIT 중심(§3)
            _stats = new CombatStats(1, new PrimaryAttributes(str: 25, dex: 15, vit: 20, ene: 10));
            _progression = new PlayerProgression(_stats);

            // 스킬 .tres 미할당 시 코드 폴백(SkillLibrary)으로 M1 검증 가능하게.
            // ★ Duplicate() 필수: .tres에서 로드한 Resource는 Godot이 경로 기준으로 캐시·공유한다.
            //   복제 없이 UpgradeSkill()로 DamageMultiplier를 직접 바꾸면, 씬 리로드(R 재시작)로
            //   새로 생긴 Player가 "이미 강화된" 공유 객체를 또 물려받고 저장된 강화 횟수를
            //   그 위에 다시 더해 이중 적용되는 버그가 생긴다(SaveLoadTest로 발견).
            StrikeSkill = (SkillData)(StrikeSkill ?? SkillLibrary.Strike()).Duplicate();
            CrushSkill = (SkillData)(CrushSkill ?? SkillLibrary.Crush()).Duplicate();
            SpinSkill = (SkillData)(SpinSkill ?? SkillLibrary.Spin()).Duplicate();

            // 저장된 진행도 복원(§ 저장/불러오기) — 스킬 인스턴스가 확정된 뒤에 적용해야 강화치가 반영됨.
            _saveEnabled = OS.HasFeature("standalone") || ForceEnableSaveForTests;
            if (_saveEnabled)
            {
                var save = SaveSystem.Load();
                if (save != null) ApplySaveData(save);
            }

            // 효과음 로드 + 레벨업 사운드 훅(저장 시점도 함께).
            _sfxSwing = GD.Load<AudioStream>("res://assets/sfx/swing.wav");
            _sfxCrush = GD.Load<AudioStream>("res://assets/sfx/crush.wav");
            _sfxHit = GD.Load<AudioStream>("res://assets/sfx/hit.wav");
            _sfxLevel = GD.Load<AudioStream>("res://assets/sfx/levelup.wav");
            _progression.LeveledUp += _ => { PlaySfx(_sfxLevel, -3f); SaveProgress(); };

            // 카메라는 뷰포트의 활성 Camera3D(= Main의 CameraRig 자식)에서 가져온다.
            _camera = GetViewport().GetCamera3D();
            if (_camera != null)
                CombatFeedback.Instance?.RegisterCamera(_camera);
        }

        // 일회성 효과음: 임시 플레이어를 만들어 재생 후 자동 해제(겹침 허용).
        private void PlaySfx(AudioStream? stream, float volumeDb = 0f)
        {
            if (stream == null) return;
            var p = new AudioStreamPlayer { Stream = stream, VolumeDb = volumeDb, Bus = "Master" };
            AddChild(p);
            p.Finished += p.QueueFree;
            p.Play();
        }

        // 공격 스윙 이펙트 표시: 부채꼴은 전방, 회전 격돌은 주위로 크게.
        private void ShowSwingVfx(SkillData skill)
        {
            if (skill.Targeting == SkillTargeting.RadialAoe)
            {
                _swingVfx.Position = new Vector3(0, 0.9f, 0);
                _swingVfx.Scale = new Vector3(1.8f, 0.35f, 1.8f);
            }
            else
            {
                _swingVfx.Position = new Vector3(0, 0.9f, -1.3f);
                _swingVfx.Scale = Vector3.One;
            }
            _swingVfx.Visible = true;
        }

        // 마우스 투영 등에서 카메라가 필요할 때 지연 획득(첫 프레임 타이밍 대비).
        private Camera3D? Cam => _camera ??= GetViewport().GetCamera3D();

        public override void _UnhandledInput(InputEvent @event)
        {
            // 사망 상태: 재시작 입력만 받는다.
            if (_dead)
            {
                if (@event.IsActionPressed("restart"))
                    GetTree().ReloadCurrentScene();
                return;
            }

            if (@event.IsActionPressed("click_move")) MoveToMouse();
            else if (@event.IsActionPressed("skill_strike")) BeginAttack(StrikeSkill);
            else if (@event.IsActionPressed("skill_crush")) BeginAttack(CrushSkill);
            else if (@event.IsActionPressed("skill_spin")) BeginAttack(SpinSkill);
            else if (@event.IsActionPressed("alloc_str")) SpendStat(StatKind.Strength);
            else if (@event.IsActionPressed("alloc_dex")) SpendStat(StatKind.Dexterity);
            else if (@event.IsActionPressed("alloc_vit")) SpendStat(StatKind.Vitality);
            else if (@event.IsActionPressed("alloc_ene")) SpendStat(StatKind.Energy);
            else if (@event.IsActionPressed("upgrade_strike")) UpgradeSkill(StrikeSkill);
            else if (@event.IsActionPressed("upgrade_crush")) UpgradeSkill(CrushSkill);
            else if (@event.IsActionPressed("upgrade_spin")) UpgradeSkill(SpinSkill);
            else return;

            GetViewport().SetInputAsHandled();
        }

        /// <summary>스탯 포인트 1개를 지정 스탯에 분배 + 저장(이전에는 다음 저장 트리거까지 유실될 수 있었음).</summary>
        public bool SpendStat(StatKind kind)
        {
            if (!_progression.SpendStatPoint(kind)) return false;
            SaveProgress();
            return true;
        }

        /// <summary>스킬 포인트 1개로 스킬 데미지 배율 강화(§5.5 스킬 포인트 사용처).</summary>
        public void UpgradeSkill(SkillData? skill)
        {
            if (skill == null) return;
            if (!_progression.SpendSkillPoint()) return;

            skill.DamageMultiplier += BalanceConstants.SkillUpgradeDamageBonus;
            _skillUpgrades[skill.Id] = _skillUpgrades.GetValueOrDefault(skill.Id) + 1;
            SaveProgress();
        }

        private SkillData? ResolveSkillById(string id)
        {
            if (StrikeSkill?.Id == id) return StrikeSkill;
            if (CrushSkill?.Id == id) return CrushSkill;
            if (SpinSkill?.Id == id) return SpinSkill;
            return null;
        }

        // ── 저장/불러오기 ─────────────────────────────────
        private void SaveProgress()
        {
            if (_saveEnabled) SaveSystem.Save(BuildSaveData());
        }

        private SaveData BuildSaveData()
        {
            var d = new SaveData
            {
                Level = _stats.Level,
                CurrentXp = _progression.CurrentXp,
                UnspentStatPoints = _progression.UnspentStatPoints,
                UnspentSkillPoints = _progression.UnspentSkillPoints,
                Str = _stats.Attributes.Strength,
                Dex = _stats.Attributes.Dexterity,
                Vit = _stats.Attributes.Vitality,
                Ene = _stats.Attributes.Energy,
                EqBonusStrength = _stats.Equipment.BonusStrength,
                EqFlatDefense = _stats.Equipment.FlatDefense,
                EqFlatAttackRating = _stats.Equipment.FlatAttackRating,
                EqFlatMaxHp = _stats.Equipment.FlatMaxHp,
                EqFlatMinDamage = _stats.Equipment.FlatMinDamage,
                EqFlatMaxDamage = _stats.Equipment.FlatMaxDamage,
            };
            foreach (var it in Inventory) d.InventoryItemIds.Add(it.Id);
            foreach (var kv in _skillUpgrades) d.SkillUpgrades[kv.Key] = kv.Value;
            return d;
        }

        private void ApplySaveData(SaveData d)
        {
            _stats.LoadState(d.Level,
                new PrimaryAttributes(d.Str, d.Dex, d.Vit, d.Ene),
                new EquipmentAggregate
                {
                    BonusStrength = d.EqBonusStrength,
                    FlatDefense = d.EqFlatDefense,
                    FlatAttackRating = d.EqFlatAttackRating,
                    FlatMaxHp = d.EqFlatMaxHp,
                    FlatMinDamage = d.EqFlatMinDamage,
                    FlatMaxDamage = d.EqFlatMaxDamage,
                });
            _progression.LoadState(d.CurrentXp, d.UnspentStatPoints, d.UnspentSkillPoints);

            Inventory.Clear();
            foreach (var id in d.InventoryItemIds)
            {
                var item = LootTable.FindById(id);
                if (item != null) Inventory.Add(item);
            }

            foreach (var kv in d.SkillUpgrades)
            {
                var skill = ResolveSkillById(kv.Key);
                if (skill == null) continue;
                skill.DamageMultiplier += BalanceConstants.SkillUpgradeDamageBonus * kv.Value;
                _skillUpgrades[kv.Key] = kv.Value;
            }
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

            _hud?.RefreshInventory(Inventory); // 인벤토리 패널(I) 갱신
            SaveProgress();
        }

        // 화면 클릭 → 지면(y=0 평면) 교차점으로 이동 목표 설정(M0).
        private void MoveToMouse()
        {
            if (TryProjectMouseToGround(out Vector3 target))
                MoveTo(target);
        }

        /// <summary>지면 목표 지점으로 이동 지시(클릭 이동 + 테스트에서 사용).</summary>
        public void MoveTo(Vector3 groundPos)
        {
            groundPos.Y = 0f;
            _moveTarget = groundPos;
            _hasMoveTarget = true;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_dead) { Velocity = Vector3.Zero; MoveAndSlide(); return; }
            HandleAttack(delta);
            HandleMovement();
            SyncHud();
        }

        private void HandleMovement()
        {
            // 공격 중이라도 스킬이 "이동하며 지속"(회전 격돌)이면 이동 허용, 아니면 스윙에 고정.
            bool rooted = _windowOpen && (_activeSkill == null || !_activeSkill.AllowMoveWhileActive);
            if (rooted) { Velocity = Vector3.Zero; MoveAndSlide(); return; }

            if (!_hasMoveTarget) { Velocity = Vector3.Zero; MoveAndSlide(); return; }

            Vector3 to = _moveTarget - GlobalPosition;
            to.Y = 0;
            if (to.Length() <= ArriveDistance)
            {
                _hasMoveTarget = false;
                Velocity = Vector3.Zero;
                MoveAndSlide();
                return;
            }

            Vector3 dir = to.Normalized();
            Velocity = dir * MoveSpeed;
            // 이동하며 지속 스킬은 이동 방향을 바라보고, 일반 공격은 타격 방향 유지.
            if (!_windowOpen || (_activeSkill?.AllowMoveWhileActive ?? false))
                FaceDirection(dir);
            MoveAndSlide();
        }

        private void BeginAttack(SkillData? skill)
        {
            if (skill == null || _activeSkill != null) return; // 이미 공격 중이면 무시(간이)
            if (!_stats.SpendMp(skill.ManaCost)) return;

            _activeSkill = skill;
            _attackTimer = 0f;
            _rehitTimer = 0f;
            _windowOpen = false;

            // 마우스 방향으로 회전(타격 방향 확정)
            FaceMouse();

            // 히트박스 형상: override>0이면 적용, 아니면 씬 기본값으로 복원(직전 스킬 값 잔류 방지).
            _hitbox.Range = skill.RangeOverride > 0 ? skill.RangeOverride : _defaultRange;
            _hitbox.ConeHalfAngleDeg = skill.ConeHalfAngleOverride > 0 ? skill.ConeHalfAngleOverride : _defaultConeHalfAngle;
        }

        private void HandleAttack(double delta)
        {
            if (_activeSkill == null) return;
            float dt = (float)delta;
            _attackTimer += dt;

            // 액티브 윈도 진입 — 스윙 이펙트 + 휘두르는 소리(적이 없어도 입력 확인 가능).
            if (!_windowOpen && _attackTimer >= _activeSkill.ActiveWindowStart)
            {
                _windowOpen = true;
                _hitbox.BeginSwing();
                ShowSwingVfx(_activeSkill);
                PlaySfx(_sfxSwing, -2f);
            }

            // 윈도 동안 매 물리프레임 Strike(캐시로 다단히트 방지, spin류는 회전하며 여러 대상)
            if (_windowOpen && _attackTimer <= _activeSkill.ActiveWindowEnd)
            {
                // 다단타: RehitInterval마다 캐시를 비워 같은 대상을 다시 타격.
                if (_activeSkill.RehitInterval > 0f)
                {
                    _rehitTimer += dt;
                    if (_rehitTimer >= _activeSkill.RehitInterval)
                    {
                        _rehitTimer -= _activeSkill.RehitInterval;
                        _hitbox.ResetHitCache();
                    }
                }

                var hits = _hitbox.Strike(BuildAttackProfile(_activeSkill), this,
                    _activeSkill.KnockbackScale, _rng);
                if (hits.Count > 0) TriggerFeedback(hits, _activeSkill);
            }

            // 윈도 종료
            if (_windowOpen && _attackTimer > _activeSkill.ActiveWindowEnd)
            {
                _hitbox.EndSwing();
                _windowOpen = false;
                _swingVfx.Visible = false;
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
                PlaySfx(skill.HeavyHitStop ? _sfxCrush : _sfxHit, 0f);
            }
        }

        // ── 회전 유틸 ─────────────────────────────────────
        private void FaceMouse()
        {
            if (TryProjectMouseToGround(out Vector3 target))
            {
                Vector3 face = target - GlobalPosition; face.Y = 0;
                if (face.LengthSquared() > 0.0001f) FaceDirection(face.Normalized());
            }
        }

        // 마우스 화면 좌표 → 지면(y=0) 교차점. 카메라 없거나 평면과 평행이면 false.
        private bool TryProjectMouseToGround(out Vector3 hit)
        {
            hit = Vector3.Zero;
            Camera3D? cam = Cam;
            if (cam == null) return false;
            Vector2 mouse = GetViewport().GetMousePosition();
            Vector3 from = cam.ProjectRayOrigin(mouse);
            Vector3 dir = cam.ProjectRayNormal(mouse);
            if (Mathf.IsZeroApprox(dir.Y)) return false;
            float t = -from.Y / dir.Y;
            if (t < 0) return false;
            hit = from + dir * t;
            return true;
        }

        private void FaceDirection(Vector3 dir)
        {
            // Godot 정면(-Z) 기준 world-forward = (-sinθ, 0, -cosθ) → dir와 일치시키려면
            // θ = atan2(-dir.X, -dir.Z) (atan2(dir.X, dir.Z)는 180° 반대를 가리킴 — 실전 검증 중 발견).
            float yaw = Mathf.Atan2(-dir.X, -dir.Z);
            Rotation = new Vector3(0, yaw, 0);
        }

        // ── IDamageable ──────────────────────────────────
        public DefenderProfile GetDefenderProfile()
            => new DefenderProfile(_stats.Defense, 0f, _stats.Level);

        public void ReceiveHit(in DamageResult result, Vector3 knockbackDir, float knockbackStrength)
        {
            if (_dead) return;

            if (!result.Hit)
            {
                DamageNumberSpawner.Instance?.Spawn(GlobalPosition, 0f, false, miss: true);
                return;
            }
            _stats.ApplyDamage(result.Amount);
            DamageNumberSpawner.Instance?.Spawn(GlobalPosition, result.Amount, result.IsCritical, miss: false);

            if (!_stats.IsAlive) Die();
        }

        private void Die()
        {
            _dead = true;
            _activeSkill = null;
            _windowOpen = false;
            _hitbox.EndSwing();
            _swingVfx.Visible = false;
            Velocity = Vector3.Zero;
            CombatFeedback.Instance?.Shake(0.2f);
            (GetTree().GetFirstNodeInGroup("hud") as Hud)?.ShowDeath();
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
