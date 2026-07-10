// tests/CaptureShot.cs
// 개발용 스크린샷 캡처 하니스(비-headless 전용). 지정 씬을 인스턴스화해 몇 프레임 렌더 후
// 뷰포트를 PNG로 저장하고 종료한다. 실제 화면이 어떻게 보이는지 눈으로 확인하기 위한 도구.
// 사용: godot --path . res://tests/CaptureShot.tscn -- --shot=<경로> --frames=90 --scene=res://scenes/Main.tscn

using Godot;
using ChaosOfAI.Actors;

namespace ChaosOfAI.Tests
{
    public partial class CaptureShot : Node
    {
        [Export] public string ScenePath = "res://scenes/Main.tscn";
        [Export] public string OutPath = "user://shot.png";
        [Export] public int WaitFrames = 90;
        [Export] public string PressAction = ""; // 캡처 직전 주입할 InputMap 액션(예: skill_strike)

        private int _frames;
        private bool _done;
        private bool _pressed;
        private Player? _player;

        public override void _Ready()
        {
            foreach (string a in OS.GetCmdlineUserArgs())
            {
                if (a.StartsWith("--shot=")) OutPath = a.Substring("--shot=".Length);
                else if (a.StartsWith("--scene=")) ScenePath = a.Substring("--scene=".Length);
                else if (a.StartsWith("--frames=") && int.TryParse(a.Substring("--frames=".Length), out int f)) WaitFrames = f;
                else if (a.StartsWith("--press=")) PressAction = a.Substring("--press=".Length);
            }

            var inst = GD.Load<PackedScene>(ScenePath).Instantiate();
            AddChild(inst);
            _player = inst.GetNodeOrNull<Player>("Player");
        }

        public override void _Process(double delta)
        {
            if (_done) return;

            // 캡처 6프레임 전에 실제 입력 이벤트를 주입 → 스윙 윈도가 열린 상태로 촬영.
            if (!_pressed && !string.IsNullOrEmpty(PressAction) && _frames >= WaitFrames - 9)
            {
                _pressed = true;
                var down = new InputEventAction { Action = PressAction, Pressed = true };
                Input.ParseInputEvent(down);
                var up = new InputEventAction { Action = PressAction, Pressed = false };
                Input.ParseInputEvent(up);
            }

            if (_frames++ < WaitFrames) return;
            _done = true;
            CallDeferred(nameof(Capture));
        }

        private async void Capture()
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var img = GetViewport().GetTexture().GetImage();
            Error err = img.SavePng(OutPath);
            string atk = _player != null ? $"attacking={_player.IsAttacking} swingVfx={_player.SwingVisible}" : "no-player";
            GD.Print($"[CaptureShot] saved={OutPath} err={err} size={img.GetWidth()}x{img.GetHeight()} {atk}");
            GetTree().Quit(err == Error.Ok ? 0 : 1);
        }
    }
}
