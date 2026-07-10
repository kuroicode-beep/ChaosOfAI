// ui/Hud.cs
// HUD(§6): 체력(빨강)·마나(파랑) 구슬 + 상시 버전 표시(전역 규칙: 화면 상단 모서리 vX.Y.Z).
// 설정 패널에 "업데이트 내역"(전역 규칙: AppVersion.History를 최신순으로 앱 안에서 바로 확인).
// toggle_settings(Esc)/toggle_inventory(I)로 패널 토글. 씬 기대치: Hud.tscn 노드 트리 주석 참고.

using System.Collections.Generic;
using System.Text;
using Godot;
using ChaosOfAI.Core;
using ChaosOfAI.Resources;

namespace ChaosOfAI.UI
{
    public partial class Hud : CanvasLayer
    {
        private Label _versionLabel = null!;
        private ProgressBar _hpOrb = null!;
        private ProgressBar _mpOrb = null!;
        private Label _levelLabel = null!;
        private Label _statPointsLabel = null!;
        private Control _settingsPanel = null!;
        private RichTextLabel _historyText = null!;
        private Control _deathPanel = null!;
        private Control _victoryPanel = null!;
        private Control _inventoryPanel = null!;
        private RichTextLabel _inventoryText = null!;

        public override void _Ready()
        {
            AddToGroup("hud"); // Player가 그룹으로 탐지해 UpdateVitals 호출
            _versionLabel = GetNode<Label>("VersionLabel");
            _hpOrb = GetNode<ProgressBar>("Vitals/HpOrb");
            _mpOrb = GetNode<ProgressBar>("Vitals/MpOrb");
            _levelLabel = GetNode<Label>("Vitals/LevelLabel");
            _statPointsLabel = GetNode<Label>("Vitals/StatPointsLabel");
            _settingsPanel = GetNode<Control>("SettingsPanel");
            _historyText = GetNode<RichTextLabel>("SettingsPanel/HistoryText");
            _deathPanel = GetNode<Control>("DeathPanel");
            _victoryPanel = GetNode<Control>("VictoryPanel");
            _inventoryPanel = GetNode<Control>("InventoryPanel");
            _inventoryText = GetNode<RichTextLabel>("InventoryPanel/ItemList");

            _versionLabel.Text = $"v{AppVersion.Current}";
            _historyText.Text = BuildHistoryText();
            _settingsPanel.Visible = false;
            _deathPanel.Visible = false;
            _victoryPanel.Visible = false;
            _inventoryPanel.Visible = false;
            RefreshInventory(System.Array.Empty<ItemData>());
        }

        /// <summary>플레이어 사망 시 오버레이 표시(§ 발견1 수정). R키 재시작 안내는 씬 리로드로 처리.</summary>
        public void ShowDeath() => _deathPanel.Visible = true;

        /// <summary>보스 처치 = 프롤로그 클리어(M5). R키로 재시작 가능.</summary>
        public void ShowVictory() => _victoryPanel.Visible = true;

        public bool VictoryVisible => _victoryPanel.Visible; // 테스트용

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event.IsActionPressed("toggle_settings"))
                _settingsPanel.Visible = !_settingsPanel.Visible;
            else if (@event.IsActionPressed("toggle_inventory"))
                _inventoryPanel.Visible = !_inventoryPanel.Visible;
            // 승리 화면에서 R = 재시작(사망 재시작은 Player가 처리).
            else if (_victoryPanel.Visible && @event.IsActionPressed("restart"))
                GetTree().ReloadCurrentScene();
        }

        /// <summary>M4 인벤토리(간소화): 습득 목록을 등급 색상 + 접사 요약으로 갱신. 픽업 시 호출.</summary>
        public void RefreshInventory(IReadOnlyList<ItemData> items)
        {
            var sb = new StringBuilder();
            sb.Append($"[b]습득 아이템 {items.Count}개[/b] (효과는 즉시 적용됨)\n\n");
            foreach (var it in items)
            {
                string hex = it.RarityColor.ToHtml(false);
                sb.Append($"[color=#{hex}]■ {it.DisplayName}[/color] [{RarityLabel(it.Rarity)}]\n");
                if (it.BonusMinDamage > 0 || it.BonusMaxDamage > 0)
                    sb.Append($"   +데미지 {it.BonusMinDamage}~{it.BonusMaxDamage}\n");
                if (it.BonusMaxHp > 0) sb.Append($"   +최대 HP {it.BonusMaxHp}\n");
                if (it.BonusStrength > 0) sb.Append($"   +힘 {it.BonusStrength}\n");
                if (it.BonusDefense > 0) sb.Append($"   +방어 {it.BonusDefense}\n");
                if (it.BonusAttackRating > 0) sb.Append($"   +명중 {it.BonusAttackRating}\n");
            }
            if (items.Count == 0) sb.Append("(아직 없음 — 몬스터를 처치해 보세요)");
            _inventoryText.Text = sb.ToString();
        }

        // 접근성: 색상만으로 등급을 구분하지 않고 텍스트 라벨 병행(전역 규칙).
        private static string RarityLabel(ItemRarity r) => r switch
        {
            ItemRarity.Normal => "일반",
            ItemRarity.Magic => "매직",
            ItemRarity.Rare => "레어",
            ItemRarity.Unique => "유니크",
            _ => "?",
        };

        /// <summary>매 프레임 또는 스탯 변경 시 호출해 구슬을 갱신.</summary>
        public void UpdateVitals(float hp, float maxHp, float mp, float maxMp)
        {
            _hpOrb.MaxValue = maxHp; _hpOrb.Value = hp;
            _mpOrb.MaxValue = maxMp; _mpOrb.Value = mp;
        }

        /// <summary>M3: 레벨/경험치/미배분 포인트 표시(§5.5 진행 루프 체감용, 그레이박스 텍스트 표기).</summary>
        public void UpdateProgression(int level, int xp, int xpToNext, int statPoints, int skillPoints)
        {
            _levelLabel.Text = $"Lv.{level}  XP {xp}/{xpToNext}";
            string stat = statPoints > 0 ? $"스탯 포인트 {statPoints} (4=STR 5=DEX 6=VIT 7=ENE)" : "";
            string skill = skillPoints > 0 ? $"스킬 포인트 {skillPoints} (8=강타 9=분쇄 0=회전격돌 강화)" : $"스킬 포인트 {skillPoints}";
            _statPointsLabel.Text = string.IsNullOrEmpty(stat) ? skill : $"{stat} · {skill}";
        }

        private static string BuildHistoryText()
        {
            var sb = new StringBuilder();
            foreach (var entry in AppVersion.History)
            {
                sb.Append("[b]v").Append(entry.Version).Append("[/b]  ").Append(entry.Date).Append('\n');
                foreach (var line in entry.Summary)
                    sb.Append("- ").Append(line).Append('\n');
                sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}
