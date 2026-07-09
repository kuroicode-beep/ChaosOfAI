// ui/Hud.cs
// HUD 스켈레톤(§6). 체력(빨강)·마나(파랑) 구슬 + 상시 버전 표시(전역 규칙: 화면 상단 모서리 vX.Y.Z).
// Sonnet 확장 지점: 구슬 채움 셰이더/텍스처, 스킬 슬롯, 설정 → "업데이트 내역" 화면(AppVersion.History 렌더).

using Godot;
using ChaosOfAI.Core;

namespace ChaosOfAI.UI
{
    public partial class Hud : CanvasLayer
    {
        [Export] public Label VersionLabel;   // 화면 상단 모서리
        [Export] public TextureProgressBar HpOrb;
        [Export] public TextureProgressBar MpOrb;

        public override void _Ready()
        {
            if (VersionLabel != null)
                VersionLabel.Text = $"v{AppVersion.Current}";
        }

        /// <summary>매 프레임 또는 스탯 변경 시 호출해 구슬을 갱신.</summary>
        public void UpdateVitals(float hp, float maxHp, float mp, float maxMp)
        {
            if (HpOrb != null) { HpOrb.MaxValue = maxHp; HpOrb.Value = hp; }
            if (MpOrb != null) { MpOrb.MaxValue = maxMp; MpOrb.Value = mp; }
        }
    }
}
