// scripts/combat/IDamageable.cs
// 피격 대상 계약. 몬스터/플레이어 노드가 구현한다. CombatSystem이 이 인터페이스로만 상호작용.

using Godot;

namespace ChaosOfAI.Combat
{
    public interface IDamageable
    {
        /// <summary>피격 시 방어 판정에 쓸 프로필(방어도/감쇠/레벨).</summary>
        DefenderProfile GetDefenderProfile();

        bool IsAlive { get; }

        /// <summary>월드 위치(넉백 방향 계산용).</summary>
        Vector3 GlobalPosition { get; }

        /// <summary>
        /// 피격 처리: 데미지 적용 + flinch/발광 + 넉백 + 데미지 넘버 요청(§4.3).
        /// result.Hit==false면 "MISS" 표시만.
        /// </summary>
        void ReceiveHit(in DamageResult result, Vector3 knockbackDir, float knockbackStrength);
    }
}
