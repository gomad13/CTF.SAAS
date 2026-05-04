"use client";

import { useCallback, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import type { CoachingFeedback, CoachingHistoryPage } from "@/lib/types/coaching";

// Aligné sur le pattern existant (TanStack Query + apiFetch). On expose un
// hook impératif pour la génération (déclenchée à l'échec d'un challenge,
// donc en réponse à un événement utilisateur, pas un useQuery automatique)
// et deux useQuery pour la lecture (single + paginé historique).

export function useGenerateCoaching() {
    const [data, setData] = useState<CoachingFeedback | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(false);

    const generate = useCallback(async (attemptId: string) => {
        setIsLoading(true);
        setError(null);
        setData(null);
        try {
            const dto = await apiFetch<CoachingFeedback>("/api/coaching/generate", {
                method: "POST",
                body: JSON.stringify({ attemptId }),
            });
            setData(dto);
            return dto;
        } catch (err) {
            const msg = err instanceof Error ? err.message : "Coaching temporairement indisponible";
            setError(msg);
            return null;
        } finally {
            setIsLoading(false);
        }
    }, []);

    const reset = useCallback(() => {
        setData(null);
        setError(null);
        setIsLoading(false);
    }, []);

    return { data, error, isLoading, generate, reset };
}

export function useCoaching(id: string | null) {
    return useQuery<CoachingFeedback>({
        queryKey: ["coaching", "single", id],
        queryFn: () => apiFetch<CoachingFeedback>(`/api/coaching/me/${id}`),
        enabled: !!id,
        staleTime: 60 * 1000,
    });
}

export function useCoachingHistory(page: number = 1, pageSize: number = 20) {
    return useQuery<CoachingHistoryPage>({
        queryKey: ["coaching", "history", page, pageSize],
        queryFn: () => apiFetch<CoachingHistoryPage>(`/api/coaching/me/history?page=${page}&pageSize=${pageSize}`),
        staleTime: 30 * 1000,
    });
}
