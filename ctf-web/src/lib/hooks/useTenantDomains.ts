"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";

export interface TenantDomain {
    id: string;
    domain: string;
    isVerified: boolean;
    status: "pending" | "verified";
    verifiedAt: string | null;
    lastCheckedAt: string | null;
    createdAt: string;
    dnsRecordName: string;
    dnsRecordValue: string;
}

export interface VerifyDomainResult {
    result: "verified" | "record_not_found" | "token_mismatch" | "dns_unavailable";
    isVerified: boolean;
    message: string;
}

const KEY = ["tenant", "domains"] as const;

/** Liste les domaines email du tenant de l'admin connecté. */
export function useTenantDomains() {
    return useQuery<TenantDomain[]>({
        queryKey: KEY,
        queryFn: () => apiFetch<TenantDomain[]>("/api/tenant/domains"),
        staleTime: 30 * 1000,
    });
}

/** Déclare un nouveau domaine (le serveur génère le token de vérification). */
export function useDeclareDomain() {
    const qc = useQueryClient();
    return useMutation<TenantDomain, Error, string>({
        mutationFn: (domain) =>
            apiFetch<TenantDomain>("/api/tenant/domains", { method: "POST", body: JSON.stringify({ domain }) }),
        onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
    });
}

/** Lance la vérification DNS d'un domaine. */
export function useVerifyDomain() {
    const qc = useQueryClient();
    return useMutation<VerifyDomainResult, Error, string>({
        mutationFn: (id) => apiFetch<VerifyDomainResult>(`/api/tenant/domains/${id}/verify`, { method: "POST" }),
        onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
    });
}

/** Retire un domaine (le token est invalidé). */
export function useRemoveDomain() {
    const qc = useQueryClient();
    return useMutation<void, Error, string>({
        mutationFn: (id) => apiFetch<void>(`/api/tenant/domains/${id}`, { method: "DELETE" }),
        onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
    });
}
