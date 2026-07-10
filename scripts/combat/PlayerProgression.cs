// scripts/combat/PlayerProgression.cs
// M3 진행 루프(§5.5): 경험치 누적 → 레벨업 → 스탯/스킬 포인트 배분(디아2식, §5.1).
// CombatStats(파생 스탯 계산)와 분리된 순수 로직 — 엔진 비의존, 테스트 가능.

using System;

namespace ChaosOfAI.Combat
{
    public enum StatKind { Strength, Dexterity, Vitality, Energy }

    public sealed class PlayerProgression
    {
        private readonly CombatStats _stats;

        public int CurrentXp { get; private set; }
        public int UnspentStatPoints { get; private set; }
        public int UnspentSkillPoints { get; private set; }

        /// <summary>레벨업 발생 시 알림(HUD 갱신 등). 인자는 새 레벨.</summary>
        public event Action<int>? LeveledUp;

        public PlayerProgression(CombatStats stats) => _stats = stats;

        public int XpToNextLevel =>
            BalanceConstants.XpCurveBase + (_stats.Level - 1) * BalanceConstants.XpCurveGrowth;

        /// <summary>경험치 지급. 임계치를 넘으면 자동으로 여러 레벨 연쇄 상승 처리.</summary>
        public void GrantXp(int amount)
        {
            if (amount <= 0) return;
            CurrentXp += amount;

            while (CurrentXp >= XpToNextLevel)
            {
                CurrentXp -= XpToNextLevel;
                _stats.LevelUp(1);
                UnspentStatPoints += BalanceConstants.StatPointsPerLevel;
                UnspentSkillPoints += BalanceConstants.SkillPointsPerLevel;
                LeveledUp?.Invoke(_stats.Level);
            }
        }

        /// <summary>스탯 포인트 1개를 지정 스탯에 분배. 포인트 없으면 false.</summary>
        public bool SpendStatPoint(StatKind kind)
        {
            if (UnspentStatPoints <= 0) return false;

            switch (kind)
            {
                case StatKind.Strength: _stats.Attributes.Strength++; break;
                case StatKind.Dexterity: _stats.Attributes.Dexterity++; break;
                case StatKind.Vitality: _stats.Attributes.Vitality++; break;
                case StatKind.Energy: _stats.Attributes.Energy++; break;
            }
            UnspentStatPoints--;
            return true;
        }

        /// <summary>스킬 포인트 1개 소비. 실제 강화 적용(SkillData 변경)은 호출자(Player) 책임.</summary>
        public bool SpendSkillPoint()
        {
            if (UnspentSkillPoints <= 0) return false;
            UnspentSkillPoints--;
            return true;
        }

        /// <summary>저장 데이터 복원용: XP/포인트를 직접 설정(레벨업 이벤트 재발화 없음).</summary>
        public void LoadState(int currentXp, int unspentStatPoints, int unspentSkillPoints)
        {
            CurrentXp = currentXp;
            UnspentStatPoints = unspentStatPoints;
            UnspentSkillPoints = unspentSkillPoints;
        }
    }
}
