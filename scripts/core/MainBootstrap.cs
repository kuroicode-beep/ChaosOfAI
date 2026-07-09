// scripts/core/MainBootstrap.cs
// M0 셋업: NavigationRegion3D 런타임 베이크(에디터 사전 베이크 대신). 바닥 지오메트리가
// 바뀌어도 항상 유효하고, 헤드리스 실행에서도 동일하게 동작한다.
// 동기(onThread:false) 베이크 — Player/Enemy의 첫 _PhysicsProcess보다 반드시 먼저 끝난다
// (모든 노드의 _Ready()가 완료된 뒤에야 첫 프레임의 _PhysicsProcess가 돌기 때문).

using Godot;

namespace ChaosOfAI.Core
{
    public partial class MainBootstrap : Node
    {
        [Export] public NodePath NavigationRegionPath = new();

        public override void _Ready()
        {
            if (NavigationRegionPath.IsEmpty) return;
            var region = GetNodeOrNull<NavigationRegion3D>(NavigationRegionPath);
            if (region == null) return;

            var navMesh = region.NavigationMesh ?? new NavigationMesh();
            // 시각 메시(MeshInstance3D)가 아니라 충돌 형상(StaticBody3D/CollisionShape3D)에서 소스 지오메트리를
            // 파싱 → GPU→CPU 메시 전송 없이 베이크(성능 경고 회피, 물리 형상과 항상 일치).
            navMesh.GeometryParsedGeometryType = NavigationMesh.ParsedGeometryType.StaticColliders;
            region.NavigationMesh = navMesh;
            region.BakeNavigationMesh(false);
        }
    }
}
