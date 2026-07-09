// ui/DamageNumber.cs
// 데미지 넘버 팝업(§4.3, §6). 가독성: 큰 폰트 + 외곽선, 크리티컬은 색/크기 강조.
// 스켈레톤 — Sonnet이 풀링(object pool)과 애니메이션(위로 떠오르며 페이드)을 구현.

using Godot;

namespace ChaosOfAI.UI
{
    public partial class DamageNumber : Node3D
    {
        [Export] public Label3D Label; // 자식 Label3D (외곽선 설정된)

        private float _lifetime = 0.8f;
        private float _age;
        private Vector3 _riseVelocity = new(0, 1.5f, 0);

        /// <summary>표시 세팅. miss=true면 "MISS", crit이면 강조 색/크기.</summary>
        public void Show(float amount, bool crit, bool miss)
        {
            if (Label == null) Label = GetNodeOrNull<Label3D>("Label3D");
            if (Label == null) return;

            if (miss)
            {
                Label.Text = "MISS";
                Label.Modulate = new Color(0.7f, 0.7f, 0.7f);
            }
            else
            {
                Label.Text = Mathf.RoundToInt(amount).ToString();
                Label.Modulate = crit ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 1f, 1f);
                Label.FontSize = crit ? 48 : 32;
            }
            // 가독성: 외곽선은 씬에서 Label3D.OutlineSize/OutlineModulate로 설정.
        }

        public override void _Process(double delta)
        {
            _age += (float)delta;
            Position += _riseVelocity * (float)delta;
            if (Label != null)
            {
                float a = Mathf.Clamp(1f - _age / _lifetime, 0f, 1f);
                var c = Label.Modulate; c.A = a; Label.Modulate = c;
            }
            if (_age >= _lifetime) QueueFree();
        }
    }
}
