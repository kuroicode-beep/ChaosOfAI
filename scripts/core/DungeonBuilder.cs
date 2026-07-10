// scripts/core/DungeonBuilder.cs
// M5 프롤로그 던전: ASCII 맵을 읽어 바닥/벽/스폰을 생성하는 데이터 주도 빌더.
// 셀 1칸 = 2m. 기호: '#'=벽, 'P'=플레이어 시작, 'E'=글리치 드론, 'W'=녹슨 보행기,
// 'B'=보스(오버마인드 사도), 그 외('.', 공백)=바닥.
// 벽은 world 레이어(1) StaticBody3D → 플레이어/몬스터 이동이 자연히 막힌다(§7.3 데이터 주도).

using Godot;
using ChaosOfAI.Actors;
using ChaosOfAI.Resources;
using ChaosOfAI.UI;

namespace ChaosOfAI.Core
{
    public partial class DungeonBuilder : Node3D
    {
        private const float Cell = 2f;
        private const float WallHeight = 2.6f;

        // 프롤로그 레이아웃: 시작방 → 복도 → 전투방 2개 → 보스방(우측 끝).
        private static readonly string[] Map =
        {
            "########################",
            "#P.....#......#....#...#",
            "#......#..E...#..W.#...#",
            "#......#......#....#.B.#",
            "#...####..E...#....#...#",
            "#...#..#......###.##...#",
            "#...#..#..E...#....#...#",
            "#......#......#..E.#####",
            "#..E...#..W...#........#",
            "#......#......#...E....#",
            "########################",
        };

        private PackedScene _enemyScene = null!;

        public override void _Ready()
        {
            _enemyScene = GD.Load<PackedScene>("res://scenes/Enemy.tscn");
            var drone = GD.Load<EnemyData>("res://data/enemies/glitch_drone.tres");
            var walker = GD.Load<EnemyData>("res://data/enemies/rust_walker.tres");
            var boss = GD.Load<EnemyData>("res://data/enemies/overmind_herald.tres");

            int rows = Map.Length;
            int cols = Map[0].Length;
            // 맵 중심이 원점 근처가 되도록 오프셋.
            Vector3 origin = new(-cols * Cell * 0.5f, 0, -rows * Cell * 0.5f);

            BuildFloor(cols, rows, origin);

            var wallMesh = new BoxMesh { Size = new Vector3(Cell, WallHeight, Cell) };
            var wallMat = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.12f, 0.13f, 0.17f),
                EmissionEnabled = true,
                Emission = new Color(0.05f, 0.25f, 0.35f),
                EmissionEnergyMultiplier = 0.35f, // 폐허 네온 느낌의 약한 윤곽광(§6 가독성)
                Roughness = 0.85f,
            };
            var wallShape = new BoxShape3D { Size = new Vector3(Cell, WallHeight, Cell) };

            Node3D? playerStart = null;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < Map[r].Length; c++)
                {
                    char ch = Map[r][c];
                    Vector3 pos = origin + new Vector3(c * Cell + Cell * 0.5f, 0, r * Cell + Cell * 0.5f);

                    switch (ch)
                    {
                        case '#':
                            AddWall(pos, wallMesh, wallMat, wallShape);
                            break;
                        case 'P':
                            playerStart = SpawnPlayer(pos);
                            break;
                        case 'E':
                            SpawnEnemy(drone, pos, scale: 1f, isBoss: false);
                            break;
                        case 'W':
                            SpawnEnemy(walker, pos, scale: 1.15f, isBoss: false);
                            break;
                        case 'B':
                            SpawnEnemy(boss, pos, scale: 1.7f, isBoss: true);
                            break;
                    }
                }
            }

            if (playerStart == null)
                GD.PushError("DungeonBuilder: 맵에 'P'(플레이어 시작)가 없습니다.");
        }

        private void BuildFloor(int cols, int rows, Vector3 origin)
        {
            // 맵 밖이 검은 void로 보이지 않도록 사방 여유를 두고 깐다(시각용, 벽이 이탈을 막음).
            const float margin = 16f;
            float w = cols * Cell + margin * 2, d = rows * Cell + margin * 2;
            origin -= new Vector3(margin, 0, margin);
            var floor = new StaticBody3D { CollisionLayer = 1, CollisionMask = 0 };
            var shape = new CollisionShape3D
            {
                Shape = new BoxShape3D { Size = new Vector3(w, 1, d) },
                Position = new Vector3(origin.X + w / 2, -0.5f, origin.Z + d / 2),
            };
            var mesh = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(w, 1, d) },
                Position = shape.Position,
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.07f, 0.07f, 0.09f),
                    Roughness = 0.9f,
                },
            };
            floor.AddChild(shape);
            floor.AddChild(mesh);
            AddChild(floor);
        }

        private void AddWall(Vector3 pos, BoxMesh mesh, StandardMaterial3D mat, BoxShape3D shape)
        {
            var body = new StaticBody3D { CollisionLayer = 1, CollisionMask = 0 };
            body.Position = pos + new Vector3(0, WallHeight * 0.5f, 0);
            body.AddChild(new CollisionShape3D { Shape = shape });
            body.AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = mat });
            AddChild(body);
        }

        private Node3D SpawnPlayer(Vector3 pos)
        {
            var player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
            AddChild(player);
            player.GlobalPosition = pos;
            return player;
        }

        private void SpawnEnemy(EnemyData data, Vector3 pos, float scale, bool isBoss)
        {
            var enemy = _enemyScene.Instantiate<EnemyAI>();
            enemy.Data = data;
            AddChild(enemy);
            enemy.GlobalPosition = pos;
            if (scale != 1f) enemy.Scale = new Vector3(scale, scale, scale);

            if (isBoss)
            {
                // 보스 처치 = 프롤로그 클리어(M5 산출물).
                enemy.Died += OnBossDied;
            }
        }

        private void OnBossDied()
        {
            (GetTree().GetFirstNodeInGroup("hud") as Hud)?.ShowVictory();
        }
    }
}
