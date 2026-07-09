// scripts/combat/MeleeHitbox.cs
// ★ 부채꼴 근접 히트박스 (PRD §4.1) ★
// 설계 결정: 정확한 "부채꼴" 프리미티브가 없으므로 광역(구/실린더) Area3D로 브로드페이즈 후
// 코드에서 각도(dot)·거리로 필터링한다. 이 방식이 억울한 헛방/부당 피격을 줄이는 정석.
//
// 사용 흐름:
//   BeginSwing()  → 애니메이션 "타격 프레임"에서 Strike() 1~n회 → EndSwing()
//   한 스윙 내 같은 적은 1회만 피격(_hitThisSwing 캐시, §4.1 다단 히트 방지).

using System;
using System.Collections.Generic;
using Godot;

namespace ChaosOfAI.Combat
{
    public partial class MeleeHitbox : Area3D
    {
        [Export] public float Range = 2.2f;          // 사거리(월드 유닛)
        [Export] public float ConeHalfAngleDeg = 60f; // 전방 부채꼴 반각(총 120°)

        // 한 스윙에서 이미 맞은 대상 캐시 → 다단 히트 방지
        private readonly HashSet<IDamageable> _hitThisSwing = new();
        private bool _swinging;

        /// <summary>스윙 시작: 히트 캐시 초기화, 감지 활성화.</summary>
        public void BeginSwing()
        {
            _hitThisSwing.Clear();
            _swinging = true;
            Monitoring = true;
        }

        /// <summary>스윙 종료: 감지 비활성화.</summary>
        public void EndSwing()
        {
            _swinging = false;
            Monitoring = false;
        }

        /// <summary>
        /// 타격 프레임에서 호출. 부채꼴 안의 유효 대상을 수집→데미지 적용하고 결과 목록을 반환한다.
        /// attacker의 -Z(정면)를 전방으로 사용.
        /// </summary>
        public List<HitApplication> Strike(in AttackProfile atk, Node3D attacker,
            float knockbackScale, Random rng)
        {
            var results = new List<HitApplication>();
            if (!_swinging) return results;

            Vector3 origin = attacker.GlobalPosition;
            Vector3 forward = -attacker.GlobalTransform.Basis.Z; // Godot 정면 = -Z
            forward.Y = 0f;
            if (forward.LengthSquared() < 0.0001f) return results;
            forward = forward.Normalized();

            float cosHalf = Mathf.Cos(Mathf.DegToRad(ConeHalfAngleDeg));
            float rangeSq = Range * Range;

            foreach (Node3D body in GetOverlappingBodies())
            {
                if (body is not IDamageable target) continue;
                if (!target.IsAlive) continue;
                if (_hitThisSwing.Contains(target)) continue;

                Vector3 to = target.GlobalPosition - origin;
                to.Y = 0f;
                if (to.LengthSquared() > rangeSq) continue;      // 사거리 밖
                if (to.LengthSquared() < 0.0001f)                // 바로 위 → 무조건 명중
                {
                    // 붙어있는 대상은 각도 필터 생략
                }
                else if (forward.Dot(to.Normalized()) < cosHalf) // 부채꼴 밖
                {
                    continue;
                }

                DamageResult dr = DamageCalculator.Resolve(atk, target.GetDefenderProfile(), rng);
                Vector3 knockDir = to.LengthSquared() > 0.0001f ? to.Normalized() : forward;
                float knockStrength = BalanceConstants.BaseKnockback * knockbackScale;

                target.ReceiveHit(dr, knockDir, dr.Hit ? knockStrength : 0f);
                _hitThisSwing.Add(target);
                results.Add(new HitApplication(target, dr));
            }

            return results;
        }
    }

    /// <summary>Strike 결과 1건(대상 + 판정). 카메라 셰이크/히트스톱 트리거 판단에 사용.</summary>
    public readonly struct HitApplication
    {
        public readonly IDamageable Target;
        public readonly DamageResult Result;
        public HitApplication(IDamageable target, DamageResult result)
        {
            Target = target; Result = result;
        }
    }
}
