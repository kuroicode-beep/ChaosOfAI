// tests/CaptureShot.cs
// 개발용 스크린샷 캡처 하니스(비-headless 전용). 지정 씬을 인스턴스화해 몇 프레임 렌더 후
// 뷰포트를 PNG로 저장하고 종료한다. 실제 화면이 어떻게 보이는지 눈으로 확인하기 위한 도구.
// 사용: godot --path . res://tests/CaptureShot.tscn -- --shot=<경로> --frames=90 --scene=res://scenes/Main.tscn

using Godot;

namespace ChaosOfAI.Tests
{
    public partial class CaptureShot : Node
    {
        [Export] public string ScenePath = "res://scenes/Main.tscn";
        [Export] public string OutPath = "user://shot.png";
        [Export] public int WaitFrames = 90;

        private int _frames;
        private bool _done;

        public override void _Ready()
        {
            foreach (string a in OS.GetCmdlineUserArgs())
            {
                if (a.StartsWith("--shot=")) OutPath = a.Substring("--shot=".Length);
                else if (a.StartsWith("--scene=")) ScenePath = a.Substring("--scene=".Length);
                else if (a.StartsWith("--frames=") && int.TryParse(a.Substring("--frames=".Length), out int f)) WaitFrames = f;
            }

            var packed = GD.Load<PackedScene>(ScenePath);
            AddChild(packed.Instantiate());
        }

        public override void _Process(double delta)
        {
            if (_done) return;
            if (_frames++ < WaitFrames) return;
            _done = true;
            CallDeferred(nameof(Capture));
        }

        private async void Capture()
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var img = GetViewport().GetTexture().GetImage();
            Error err = img.SavePng(OutPath);
            GD.Print($"[CaptureShot] saved={OutPath} err={err} size={img.GetWidth()}x{img.GetHeight()}");
            GetTree().Quit(err == Error.Ok ? 0 : 1);
        }
    }
}
