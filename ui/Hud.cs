// ui/Hud.cs
// HUD(§6): 체력(빨강)·마나(파랑) 구슬 + 상시 버전 표시(전역 규칙: 화면 상단 모서리 vX.Y.Z).
// 설정 패널에 "업데이트 내역"(전역 규칙: AppVersion.History를 최신순으로 앱 안에서 바로 확인).
// toggle_settings(Esc)로 패널 토글. 씬 기대치: Hud.tscn 노드 트리 주석 참고.

using System.Text;
using Godot;
using ChaosOfAI.Core;

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

            _versionLabel.Text = $"v{AppVersion.Current}";
            _historyText.Text = BuildHistoryText();
            _settingsPanel.Visible = false;
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (Input.IsActionJustPressed("toggle_settings"))
                _settingsPanel.Visible = !_settingsPanel.Visible;
        }

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
            _statPointsLabel.Text = statPoints > 0
                ? $"스탯 포인트 {statPoints} (4=STR 5=DEX 6=VIT 7=ENE) · 스킬 포인트 {skillPoints}"
                : $"스킬 포인트 {skillPoints}";
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
