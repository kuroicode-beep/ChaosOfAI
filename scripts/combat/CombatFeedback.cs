// scripts/combat/CombatFeedback.cs
// 타격감 피드백 허브(§4.3): 히트스톱 + 카메라 셰이크. Autoload 싱글턴으로 등록해 어디서든 호출.
// project.godot [autoload] CombatFeedback="*res://scripts/combat/CombatFeedback.cs" 로 등록.
// 데미지 넘버 팝업은 DamageNumber(UI)에서 처리하고, 여기서는 시간/카메라만 다룬다.

using Godot;

namespace ChaosOfAI.Combat
{
    public partial class CombatFeedback : Node
    {
        public static CombatFeedback? Instance { get; private set; }

        // 카메라 셰이크 대상은 활성 카메라가 등록. Player가 시작 시 등록.
        private Camera3D? _camera;
        private float _shakeAmount;
        private float _shakeDecay = 8f;
        private Vector3 _cameraBasePos;

        // 히트스톱: 벽시계(TimeScale 비의존) 종료 시각으로 관리 → async 경합 없이 다중 처치 안전.
        private bool _hitStopActive;
        private ulong _hitStopEndMsec;

        public override void _Ready()
        {
            Instance = this;
            ProcessMode = ProcessModeEnum.Always; // 히트스톱(TimeScale≈0) 중에도 _Process 동작
        }

        public void RegisterCamera(Camera3D? cam)
        {
            _camera = cam;
            if (cam != null) _cameraBasePos = cam.Position;
        }

        /// <summary>히트스톱: 잠깐 게임 시간 정지 → 묵직함. heavy=강타/처치용 긴 정지.
        /// 겹쳐 호출되면 더 늦은 종료 시각으로 연장(경합/조기 복원 없음).</summary>
        public void HitStop(bool heavy = false)
        {
            float dur = heavy ? BalanceConstants.HeavyHitStopSeconds : BalanceConstants.HitStopSeconds;
            ulong end = Time.GetTicksMsec() + (ulong)(dur * 1000f);
            if (!_hitStopActive || end > _hitStopEndMsec)
                _hitStopEndMsec = end;
            _hitStopActive = true;
            Engine.TimeScale = 0.0001f; // 완전 0은 일부 처리 이슈 → 극소값
        }

        /// <summary>카메라 셰이크 트리거. 강타·처치 시 미세 흔들림.</summary>
        public void Shake(float amount)
        {
            _shakeAmount = Mathf.Max(_shakeAmount, amount);
        }

        public override void _Process(double delta)
        {
            // 히트스톱 복원(벽시계 기준). TimeScale에 영향받지 않는 Time.GetTicksMsec 사용.
            if (_hitStopActive && Time.GetTicksMsec() >= _hitStopEndMsec)
            {
                _hitStopActive = false;
                Engine.TimeScale = 1f;
            }

            if (_camera == null || _shakeAmount <= 0.001f) return;
            float a = _shakeAmount;
            var offset = new Vector3(
                (float)GD.RandRange(-a, a),
                (float)GD.RandRange(-a, a),
                0f);
            _camera.Position = _cameraBasePos + offset;
            _shakeAmount = Mathf.Lerp(_shakeAmount, 0f, (float)delta * _shakeDecay);
            if (_shakeAmount <= 0.001f) _camera.Position = _cameraBasePos;
        }
    }
}
