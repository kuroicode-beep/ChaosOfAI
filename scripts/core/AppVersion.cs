// scripts/core/AppVersion.cs
// 전역 버전 규칙(CLAUDE.md / VERSIONING.md). APP_VERSION 상수 + VERSION_HISTORY 리스트로 관리하고
// 기능 추가 시마다 갱신한다. 설정 화면 "업데이트 내역" 메뉴가 이 리스트를 최신순으로 표시.

using System.Collections.Generic;

namespace ChaosOfAI.Core
{
    public readonly struct VersionEntry
    {
        public readonly string Version;
        public readonly string Date;      // YYYY-MM-DD
        public readonly string[] Summary; // 버전당 2~4줄
        public VersionEntry(string version, string date, string[] summary)
        {
            Version = version; Date = date; Summary = summary;
        }
    }

    public static class AppVersion
    {
        // 루트 VERSION 파일과 항상 일치시킨다.
        public const string Current = "0.4.0";

        /// <summary>최신이 앞. 설정 → 업데이트 내역에서 그대로 노출.</summary>
        public static readonly IReadOnlyList<VersionEntry> History = new List<VersionEntry>
        {
            new VersionEntry("0.4.0", "2026-07-10", new[]
            {
                "검수 지적 5건 수정: 플레이어 사망 처리(R키 재시작), 회전 격돌 이동+다단타.",
                "히트스톱 벽시계 기반 리팩터(다중 처치 경합 제거), 히트박스 형상 복원, 입력 이벤트화.",
                "Windows 실행 파일(.exe) 빌드 + 바탕화면 바로가기 생성.",
                "사망 경로 회귀 테스트 추가 — headless 테스트 4종 전부 통과.",
            }),
            new VersionEntry("0.3.0", "2026-07-10", new[]
            {
                "M3 경험치/레벨업(PlayerProgression) + 스탯 포인트 분배(4/5/6/7키) 구현.",
                "M4 아이템 드랍/장비 보너스(간소화: 그리드 UI 없이 처치 즉시 적용) 구현.",
                "HUD에 레벨·XP·포인트 표시 추가.",
                "headless 회귀 테스트 3종(로직 9건 + 진행루프 14건 + 전투 통합 2단계) 전부 통과.",
            }),
            new VersionEntry("0.2.0", "2026-07-10", new[]
            {
                "M0~M1 씬 구성(Player/Enemy/Main) + 등각 카메라 + 런타임 내비 베이크.",
                "M2 몬스터 AI(추격→공격 상태머신) 구현.",
                "HUD(체력·마나 구슬, 업데이트 내역 패널) + 데미지 넘버 연동.",
                "headless 자동 테스트 2종(로직 9건 + 전투 통합) 전부 통과 확인.",
            }),
            new VersionEntry("0.1.0", "2026-07-10", new[]
            {
                "프로젝트 초기화, Godot 4 + C# 뼈대 구성.",
                "핵심 전투 판정·데미지·스탯 로직(§4, §5) 구현.",
                "데이터 주도 설계(스킬/아이템/몬스터 .tres) 기반 마련.",
            }),
        };
    }
}
