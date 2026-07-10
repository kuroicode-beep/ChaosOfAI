// scripts/core/CameraFollow.cs
// 등각 카메라(Camera3D에 직접 부착). 플레이어를 고정 오프셋으로 추종하고 LookAt으로 바라본다.
// ★ 카메라를 회전하는 Player의 자식으로 두면 화면이 통째로 돌아 조작·가독성이 붕괴된다.
//   그래서 Player 밖(Main)에 두고 위치만 추종 + 항상 플레이어를 바라본다.
// 셰이크는 위치/회전과 충돌하지 않도록 Camera3D의 HOffset/VOffset(프러스텀 오프셋)으로 처리
//   → CombatFeedback이 담당. 여기서는 추종/시선만 다룬다.

using Godot;

namespace ChaosOfAI.Core
{
    public partial class CameraFollow : Camera3D
    {
        [Export] public NodePath TargetPath = new();
        [Export] public Vector3 Offset = new(0, 16, 12); // 위 16, 뒤(+Z) 12 → 약 53° 부감
        [Export] public float SmoothSpeed = 8f;          // 0 이하이면 즉시 추종

        private Node3D? _target;

        public override void _Ready()
        {
            if (TargetPath != null && !TargetPath.IsEmpty)
                _target = GetNodeOrNull<Node3D>(TargetPath);
            _target ??= GetTree().GetFirstNodeInGroup("player") as Node3D;

            if (_target != null)
            {
                GlobalPosition = _target.GlobalPosition + Offset;
                LookAt(_target.GlobalPosition, Vector3.Up);
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            _target ??= GetTree().GetFirstNodeInGroup("player") as Node3D;
            if (_target == null) return;

            Vector3 goal = _target.GlobalPosition + Offset;
            if (SmoothSpeed <= 0f)
                GlobalPosition = goal;
            else
                GlobalPosition = GlobalPosition.Lerp(goal, Mathf.Clamp((float)delta * SmoothSpeed, 0f, 1f));

            LookAt(_target.GlobalPosition, Vector3.Up);
        }
    }
}
