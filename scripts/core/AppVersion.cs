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
        public const string Current = "0.9.1";

        /// <summary>최신이 앞. 설정 → 업데이트 내역에서 그대로 노출.</summary>
        public static readonly IReadOnlyList<VersionEntry> History = new List<VersionEntry>
        {
            new VersionEntry("0.9.1", "2026-07-12", new[]
            {
                "벽-배경 가독성 개선: 벽 색상을 훨씬 밝게 + 카툰풍 검은 외곽선(인버티드 헐 셰이더) 추가.",
                "조명·색상과 무관하게 벽 윤곽이 항상 보이도록 처리(§6 접근성: 배경-오브젝트 구분).",
            }),
            new VersionEntry("0.9.0", "2026-07-10", new[]
            {
                "몬스터 사망 연출: 아트 자산 없이 코드만으로 축소+페이드아웃 후 소멸(즉시 QueueFree 대신).",
                "아이템 습득 사운드 추가(합성 SFX).",
                "죽음 연출 지연을 반영해 회귀 테스트 타이밍 보정, 9종 전부 통과 유지.",
            }),
            new VersionEntry("0.8.0", "2026-07-10", new[]
            {
                "바닥 드랍 픽업(§5.4): 처치 즉시 적용 대신 바닥에 아이템이 떨어지고 걸어가 습득.",
                "치명 버그 2건 수정: 스폰 시 물리 서버 레이스로 캐릭터가 서로 밀리던 문제,",
                "캐릭터 정면(FaceDirection)이 180도 반대를 보던 문제(플레이어 공격이 거의 항상 빗나갔음).",
                "회귀 테스트 2종 추가(PlayerAttackTest·ItemPickupTest), 총 9종 전부 통과.",
            }),
            new VersionEntry("0.7.0", "2026-07-10", new[]
            {
                "저장/불러오기: 레벨·XP·스탯·장비·인벤토리·스킬강화가 재시작 후에도 이어짐.",
                "스킬 포인트 실사용: 8/9/0 키로 강타/분쇄/회전격돌 강화(데미지 증가).",
                "세이브는 실행 파일에서만 동작 — 개발 테스트가 실제 진행도를 건드리지 않음.",
            }),
            new VersionEntry("0.6.0", "2026-07-10", new[]
            {
                "인벤토리 패널(I 키): 습득 아이템 목록 — 등급 색상+텍스트 라벨, 접사 요약.",
                "몬스터 피격 발광(flinch): 명중 시 흰색 플래시(개체별 머티리얼).",
            }),
            new VersionEntry("0.5.0", "2026-07-10", new[]
            {
                "M5 프롤로그 던전: 방·복도·벽 레이아웃(ASCII 맵 데이터 주도) + 적 배치.",
                "보스 '오버마인드의 사도' + 처치 시 프롤로그 클리어 화면(R 재시작).",
                "벽 관통 어그로 방지(시야 판정). 던전이 시작 씬으로 변경.",
            }),
            new VersionEntry("0.4.2", "2026-07-10", new[]
            {
                "공격 피드백 추가: 스킬 발동 시 스윙 이펙트(전방/주위) + 효과음(휘두름·타격·레벨업).",
                "적이 없어도 1/2/3 입력이 눈·귀로 확인되도록 개선.",
                "몬스터 탐지 범위 확대(전부 덤벼옴). 실제 키 입력 시뮬레이션+스크린샷으로 발동 검증.",
            }),
            new VersionEntry("0.4.1", "2026-07-10", new[]
            {
                "치명 버그 수정: 카메라가 플레이어와 함께 회전해 화면이 붕괴되던 문제 → 회전 독립 추종.",
                "이동 불능 수정: navmesh 높이 차로 경로가 멈추던 문제 → 평지 직접 이동으로 전환(플레이어·몬스터).",
                "실제 창 렌더 확인(스크린샷) + 이동 회귀 테스트(MovementTest) 추가.",
            }),
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
