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
        public const string Current = "0.1.0";

        /// <summary>최신이 앞. 설정 → 업데이트 내역에서 그대로 노출.</summary>
        public static readonly IReadOnlyList<VersionEntry> History = new List<VersionEntry>
        {
            new VersionEntry("0.1.0", "2026-07-10", new[]
            {
                "프로젝트 초기화, Godot 4 + C# 뼈대 구성.",
                "핵심 전투 판정·데미지·스탯 로직(§4, §5) 구현.",
                "데이터 주도 설계(스킬/아이템/몬스터 .tres) 기반 마련.",
            }),
        };
    }
}
