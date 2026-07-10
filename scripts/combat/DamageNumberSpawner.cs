// scripts/combat/DamageNumberSpawner.cs
// 데미지 넘버 팝업 스폰 허브(§4.3). Autoload 싱글턴 — 어디서든 Spawn 호출.
// project.godot [autoload]에 등록됨.

using Godot;
using ChaosOfAI.UI;

namespace ChaosOfAI.Combat
{
    public partial class DamageNumberSpawner : Node
    {
        public static DamageNumberSpawner? Instance { get; private set; }

        private PackedScene? _scene;

        public override void _Ready()
        {
            Instance = this;
            _scene = GD.Load<PackedScene>("res://ui/DamageNumber.tscn");
        }

        /// <summary>월드 위치 근처에 데미지 넘버를 띄운다. miss=true면 amount 무시하고 "MISS" 표시.</summary>
        public void Spawn(Vector3 worldPos, float amount, bool crit, bool miss)
        {
            if (_scene == null) return;
            var currentScene = GetTree().CurrentScene;
            if (currentScene == null) return;

            var inst = _scene.Instantiate<DamageNumber>();
            inst.Position = worldPos + new Vector3(0, 1.6f, 0); // AddChild 전에 설정
            currentScene.AddChild(inst);
            inst.Show(amount, crit, miss);
        }
    }
}
