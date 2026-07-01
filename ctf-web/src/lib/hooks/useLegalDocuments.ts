"use client";

import { useQuery } from "@tanstack/react-query";
import type { LegalDocumentSummary } from "@/lib/types/legal";

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "";

async function fetchActiveDocuments(): Promise<LegalDocumentSummary[]> {
    const r = await fetch(`${API_BASE}/api/legal/documents`, { credentials: "include" });
    if (!r.ok) throw new Error(`HTTP ${r.status}`);
    return r.json();
}

/**
 * Récupère la liste des documents légaux actifs (publics, sans auth).
 * Utilisé par la page d'inscription et l'écran de re-acceptation pour
 * connaître la version courante à présenter à l'utilisateur. Cache 5 min
 * (les documents changent très rarement).
 */
export function useLegalDocuments() {
    const q = useQuery<LegalDocumentSummary[]>({
        queryKey: ["legal", "documents"],
        queryFn: fetchActiveDocuments,
        staleTime: 5 * 60 * 1000,
    });
    return {
        documents: q.data ?? [],
        loading: q.isLoading,
        error: q.error instanceof Error ? q.error.message : null,
    };
}
