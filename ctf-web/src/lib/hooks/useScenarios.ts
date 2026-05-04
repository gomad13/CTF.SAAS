"use client";

import { useCallback, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import type {
    ScenarioCatalogItem,
    ScenarioCatalogDetail,
    ScenarioInstanceListItem,
    ScenarioInstanceDetail,
    InboxEmailListItem,
    InboxEmailDetail,
    EligibleSender,
    EmployeeWithConsent,
    LaunchScenarioRequest,
    ReportPhishingResponse,
    ScenarioLandingDetail,
    SenderConsent,
} from "@/lib/types/scenarios";

// ── Admin ────────────────────────────────────────────────────────────────────

export function useScenarioCatalog() {
    return useQuery<ScenarioCatalogItem[]>({
        queryKey: ["scenarios", "catalog"],
        queryFn: () => apiFetch<ScenarioCatalogItem[]>("/api/admin/scenarios/catalog"),
        staleTime: 5 * 60 * 1000,
    });
}

export function useScenarioCatalogDetail(templateId: string | null) {
    return useQuery<ScenarioCatalogDetail>({
        queryKey: ["scenarios", "catalog", templateId],
        queryFn: () => apiFetch<ScenarioCatalogDetail>(`/api/admin/scenarios/catalog/${templateId}`),
        enabled: !!templateId,
        staleTime: 5 * 60 * 1000,
    });
}

export function useScenarioInstances(autoRefresh = false) {
    return useQuery<ScenarioInstanceListItem[]>({
        queryKey: ["scenarios", "instances"],
        queryFn: () => apiFetch<ScenarioInstanceListItem[]>("/api/admin/scenarios/instances"),
        refetchInterval: autoRefresh ? 10_000 : false,
        staleTime: 5_000,
    });
}

export function useScenarioInstance(id: string | null) {
    return useQuery<ScenarioInstanceDetail>({
        queryKey: ["scenarios", "instance", id],
        queryFn: () => apiFetch<ScenarioInstanceDetail>(`/api/admin/scenarios/instances/${id}`),
        enabled: !!id,
        staleTime: 5_000,
    });
}

export function useEligibleSenders() {
    return useQuery<EligibleSender[]>({
        queryKey: ["scenarios", "eligible-senders"],
        queryFn: () => apiFetch<EligibleSender[]>("/api/admin/scenarios/eligible-senders"),
        staleTime: 60_000,
    });
}

// Liste complète des employés du tenant + flag de consentement.
// Utilisé par le wizard de lancement pour afficher TOUS les employés avec
// un badge ✅/❌ et désactiver les non-consentants côté expéditeur.
export function useEmployeesWithConsent() {
    return useQuery<EmployeeWithConsent[]>({
        queryKey: ["scenarios", "employees-with-consent"],
        queryFn: () => apiFetch<EmployeeWithConsent[]>("/api/admin/scenarios/employees"),
        staleTime: 60_000,
    });
}

export function useLaunchScenario() {
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const launch = useCallback(async (req: LaunchScenarioRequest) => {
        setIsLoading(true); setError(null);
        try {
            const r = await apiFetch<{ instanceId: string }>("/api/admin/scenarios/launch", {
                method: "POST",
                body: JSON.stringify(req),
            });
            return r;
        } catch (err) {
            const msg = err instanceof Error ? err.message : "Erreur de lancement";
            setError(msg);
            return null;
        } finally { setIsLoading(false); }
    }, []);
    return { launch, isLoading, error };
}

export function useStopInstance() {
    const [isLoading, setIsLoading] = useState(false);
    const stop = useCallback(async (id: string, reason: string) => {
        setIsLoading(true);
        try {
            await apiFetch<void>(`/api/admin/scenarios/instances/${id}/stop`, {
                method: "POST",
                body: JSON.stringify({ reason }),
            });
            return true;
        } catch { return false; }
        finally { setIsLoading(false); }
    }, []);
    return { stop, isLoading };
}

// ── Inbox employé ────────────────────────────────────────────────────────────

export function useInbox(autoRefresh = false) {
    return useQuery<InboxEmailListItem[]>({
        queryKey: ["inbox", "me"],
        queryFn: () => apiFetch<InboxEmailListItem[]>("/api/inbox/me"),
        refetchInterval: autoRefresh ? 15_000 : false,
        staleTime: 5_000,
    });
}

export function useInboxEmail(id: string | null) {
    return useQuery<InboxEmailDetail>({
        queryKey: ["inbox", "email", id],
        queryFn: () => apiFetch<InboxEmailDetail>(`/api/inbox/me/${id}`),
        enabled: !!id,
        staleTime: 30_000,
    });
}

export function useReportPhishing() {
    const [isLoading, setIsLoading] = useState(false);
    const report = useCallback(async (id: string) => {
        setIsLoading(true);
        try {
            const r = await apiFetch<ReportPhishingResponse>(`/api/inbox/me/${id}/report`, {
                method: "POST",
            });
            return r;
        } catch (err) {
            const msg = err instanceof Error ? err.message : "Erreur de signalement";
            return { success: false, triggeredOutcome: false, outcomeKey: "", message: msg } as ReportPhishingResponse;
        } finally { setIsLoading(false); }
    }, []);
    return { report, isLoading };
}

// ── Landing post-clic ────────────────────────────────────────────────────────

export function useScenarioLanding(token: string | null) {
    return useQuery<ScenarioLandingDetail>({
        queryKey: ["scenarios", "landing", token],
        queryFn: () => apiFetch<ScenarioLandingDetail>(`/api/scenarios/landing/${token}`),
        enabled: !!token,
        staleTime: 60_000,
    });
}

// ── Consentement expéditeur fictif (employé) ────────────────────────────────

export function useSenderConsent() {
    return useQuery<SenderConsent>({
        queryKey: ["sender-consent", "me"],
        queryFn: () => apiFetch<SenderConsent>("/api/users/me/sender-consent"),
        staleTime: 60_000,
    });
}

export function useUpdateSenderConsent() {
    const [isLoading, setIsLoading] = useState(false);
    const update = useCallback(async (consent: boolean) => {
        setIsLoading(true);
        try {
            const r = await apiFetch<SenderConsent>("/api/users/me/sender-consent", {
                method: "PUT",
                body: JSON.stringify({ consentsToBeFictionalSender: consent }),
            });
            return r;
        } finally { setIsLoading(false); }
    }, []);
    return { update, isLoading };
}
