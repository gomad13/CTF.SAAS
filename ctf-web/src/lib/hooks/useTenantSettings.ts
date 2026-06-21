"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import type { TenantSettings, UpdateTenantSettings } from "@/lib/types/tenantSettings";

const KEY = ["tenant", "settings"] as const;

/** Lit les paramètres entreprise du tenant de l'admin connecté. */
export function useTenantSettings() {
    return useQuery<TenantSettings>({
        queryKey: KEY,
        queryFn: () => apiFetch<TenantSettings>("/api/tenant/settings"),
        staleTime: 60 * 1000,
    });
}

/** Met à jour les paramètres entreprise (whitelist serveur). */
export function useUpdateTenantSettings() {
    const qc = useQueryClient();
    return useMutation<{ success: boolean }, Error, UpdateTenantSettings>({
        mutationFn: (body) =>
            apiFetch<{ success: boolean }>("/api/tenant/settings", {
                method: "PUT",
                body: JSON.stringify(body),
            }),
        onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
    });
}
