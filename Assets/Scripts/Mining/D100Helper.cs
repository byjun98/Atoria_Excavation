namespace Rebuild.Mining
{
    /// <summary>.C (2026-05-16) — D100 talent helper (compat pass-through).
    /// <para>.C 사용자 결정 "단순화 다 풀고 원본 스케일 일치" — 이전 +0.5pp flat bonus 모델
    /// (인지 불가) 폐기, 원본 SetRockScreen:1021-1039 의 "세션 종료 시 1/10/100 (=3%) 면 ore ×3"
    /// 모델로 복원. 실제 D100 효과는 <see cref="GoldAndOreMechanics.CraftSessionToBars"/> 안에서
    /// 처리. D100Helper 의 Roll/Bonus 는 호환성 위해 pass-through 로 유지 (call site 변경 0).</para>
    /// <para>RecordD100Roll 은 더 이상 매 chance roll 마다 fire 하지 않음 — 세션 종료 시
    /// CraftSessionToBars 가 한 번 fire (lottery 발동 시). 이전 모델에서 14-artifact loop / 매 click
    /// 마다 stat 폭증 문제도 자연스럽게 해결.</para>
    /// </summary>
    public static class D100Helper
    {
        /// <summary>.C — D100 효과는 lottery 모델로 이전. 이 상수는 더 이상 사용 안 함
        /// (호환성 위해 0 으로 유지).</summary>
        public const float D100BonusPP = 0f;

        /// <summary>.C — pass-through. 이전엔 D100 talent 보유 시 +0.5pp 추가했지만
        /// 원본 모델 (session lottery) 로 이전하면서 chance 자체 변형은 제거.</summary>
        public static float Bonus(float chance) => chance;

        /// <summary>.C — pass-through. 이전엔 D100 talent 보유 시 +0.5pp + stat fire.
        /// 원본 모델 (session lottery, GoldAndOreMechanics.CraftSessionToBars) 로 이전.</summary>
        public static bool Roll(float chance) => UnityEngine.Random.value < chance;
    }
}
