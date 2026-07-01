"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";

export type AllModesStatus = {
    competition: boolean;
    analytics: boolean;
    compliance: boolean;
    teams: boolean;
    campaigns: boolean;
};

const DEFAULTS: AllModesStatus = {
    competition: false,
    analytics: false,
    compliance: false,
    teams: false,
    campaigns: false,
};

export function useModesStatus() {
    return useQuery<AllModesStatus>({
        queryKey: ["modes", "all"],
        queryFn: () => apiFetch<AllModesStatus>("/api/modes/all"),
        staleTime: 30_000,
        refetchOnWindowFocus: false,
        placeholderData: DEFAULTS,
    });
}
