// scripts/combat/ItemPickupSpawner.cs
// 바닥 드랍 스폰 허브. DamageNumberSpawner와 동일한 패턴의 Autoload 싱글턴.

using Godot;
using ChaosOfAI.Resources;

namespace ChaosOfAI.Combat
{
    public partial class ItemPickupSpawner : Node
    {
        public static ItemPickupSpawner? Instance { get; private set; }

        private PackedScene? _scene;

        public override void _Ready()
        {
            Instance = this;
            _scene = GD.Load<PackedScene>("res://ui/ItemPickup.tscn");
        }

        public void Spawn(ItemData item, Vector3 worldPos)
        {
            if (_scene == null) return;
            var currentScene = GetTree().CurrentScene;
            if (currentScene == null) return;

            var inst = _scene.Instantiate<ItemPickup>();
            inst.Item = item;
            inst.Position = worldPos; // AddChild 전에 설정(스폰 직후 첫 프레임 잘못된 위치의 겹침 판정 방지)
            currentScene.AddChild(inst);
        }
    }
}
