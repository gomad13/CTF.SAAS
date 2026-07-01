"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";

export type CompetitionStatus = { isEnabled: boolean };

export function useCompetitionStatus() {
    return useQuery<CompetitionStatus>({
        queryKey: ["competition", "status"],
        queryFn: () => apiFetch<CompetitionStatus>("/api/competition/status"),
        staleTime: 30_000,
        refetchOnWindowFocus: false,
    });
}
