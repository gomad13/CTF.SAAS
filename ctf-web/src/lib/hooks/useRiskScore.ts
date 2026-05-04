"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import type { RiskScore, RiskScoreHistoryPoint } from "@/lib/types/riskScore";

// Pattern aligné sur les autres hooks du dashboard (TanStack Query + apiFetch).
// Cache 5 min côté query, refetch silencieux à la mise au focus.

export function useRiskScore() {
    return useQuery<RiskScore>({
        queryKey: ["risk-score", "me"],
        queryFn: () => apiFetch<RiskScore>("/api/risk-score/me"),
        staleTime: 5 * 60 * 1000,
    });
}

export function useRiskScoreHistory(months: number = 6) {
    return useQuery<RiskScoreHistoryPoint[]>({
        queryKey: ["risk-score", "history", months],
        queryFn: () => apiFetch<RiskScoreHistoryPoint[]>(`/api/risk-score/me/history?months=${months}`),
        staleTime: 5 * 60 * 1000,
    });
}
