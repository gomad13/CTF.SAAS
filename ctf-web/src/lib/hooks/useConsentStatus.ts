"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import type { ConsentStatus } from "@/lib/types/legal";

/**
 * Vérifie si l'utilisateur connecté a accepté la dernière version de tous
 * les documents requis. Cache court (1 min) pour rafraîchir vite après une
 * re-acceptation.
 */
export function useConsentStatus() {
    const q = useQuery<ConsentStatus>({
        queryKey: ["legal", "consent-status"],
        queryFn: () => apiFetch<ConsentStatus>("/api/me/consents/status"),
        staleTime: 60 * 1000,
        retry: false,
    });
    return {
        status: q.data ?? null,
        loading: q.isLoading,
        error: q.error instanceof Error ? q.error.message : null,
        refetch: q.refetch,
    };
}
