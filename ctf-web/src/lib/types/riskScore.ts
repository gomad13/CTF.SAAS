// Types alignés sur les DTOs C# du backend (Contracts/RiskScore/RiskScoreDtos.cs).
// Sérialisation JSON .NET par défaut : PascalCase → camelCase.

export type RiskScoreComponents = {
    successRate: number;
    speedScore: number;
    diversityScore: number;
    regressionScore: number;
};

export type RiskScore = {
    score: number | null;
    components: RiskScoreComponents;
    computedAt: string; // ISO 8601 UTC
};

export type RiskScoreHistoryPoint = {
    date: string;        // ISO 8601 UTC
    score: number | null;
};

export type RiskLevel = "insufficient" | "vulnerable" | "progressing" | "solid" | "excellent";

export const RISK_LEVEL_LABELS: Record<RiskLevel, string> = {
    insufficient: "Données insuffisantes",
    vulnerable: "Vulnérable",
    progressing: "En progression",
    solid: "Solide",
    excellent: "Excellent",
};
