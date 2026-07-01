"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import type {
    AssignEmployeesPayload,
    AvailableContent,
    CampaignDashboard,
    CampaignDetail,
    CampaignStatus,
    CampaignSummary,
    CreateCampaignPayload,
    EmployeeCampaign,
    UpdateCampaignPayload,
} from "@/lib/types/campaigns";

const SOMETIMES = 30 * 1000; // 30 s — assez court pour refléter une assignation récente
const RARELY = 5 * 60 * 1000; // 5 min — catalogue de contenus

export function useCampaignsStatus() {
    return useQuery<{ isEnabled: boolean }>({
        queryKey: ["campaigns", "status"],
        queryFn: () => apiFetch("/api/campaigns/status"),
        staleTime: RARELY,
    });
}

export function useCampaigns(filters?: { status?: CampaignStatus | "All" }) {
    const status = filters?.status && filters.status !== "All" ? filters.status : null;
    return useQuery<CampaignSummary[]>({
        queryKey: ["campaigns", "list", status],
        queryFn: () => {
            const q = status ? `?status=${encodeURIComponent(status)}` : "";
            return apiFetch<CampaignSummary[]>(`/api/admin/campaigns${q}`);
        },
        staleTime: SOMETIMES,
    });
}

export function useCampaign(id: string | null) {
    return useQuery<CampaignDetail>({
        queryKey: ["campaigns", "detail", id],
        queryFn: () => apiFetch<CampaignDetail>(`/api/admin/campaigns/${id}`),
        enabled: !!id,
        staleTime: SOMETIMES,
    });
}

export function useCampaignDashboard(id: string | null) {
    return useQuery<CampaignDashboard>({
        queryKey: ["campaigns", "dashboard", id],
        queryFn: () => apiFetch<CampaignDashboard>(`/api/admin/campaigns/${id}/dashboard`),
        enabled: !!id,
        staleTime: SOMETIMES,
    });
}

export function useAvailableContent() {
    return useQuery<AvailableContent[]>({
        queryKey: ["campaigns", "available-content"],
        queryFn: () => apiFetch<AvailableContent[]>("/api/admin/campaigns/available-content"),
        staleTime: RARELY,
    });
}

export function useCreateCampaign() {
    const qc = useQueryClient();
    return useMutation<CampaignDetail, Error, CreateCampaignPayload>({
        mutationFn: (payload) => apiFetch<CampaignDetail>("/api/admin/campaigns", {
            method: "POST",
            body: JSON.stringify(payload),
        }),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ["campaigns", "list"] });
        },
    });
}

export function useUpdateCampaign(id: string | null) {
    const qc = useQueryClient();
    return useMutation<CampaignDetail, Error, UpdateCampaignPayload>({
        mutationFn: (payload) => apiFetch<CampaignDetail>(`/api/admin/campaigns/${id}`, {
            method: "PUT",
            body: JSON.stringify(payload),
        }),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ["campaigns"] });
        },
    });
}

export function useDeleteCampaign() {
    const qc = useQueryClient();
    return useMutation<void, Error, string>({
        mutationFn: (id) => apiFetch(`/api/admin/campaigns/${id}`, { method: "DELETE" }),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ["campaigns", "list"] });
        },
    });
}

export function useAssignEmployees(id: string | null) {
    const qc = useQueryClient();
    return useMutation<void, Error, AssignEmployeesPayload>({
        mutationFn: (payload) => apiFetch(`/api/admin/campaigns/${id}/assign`, {
            method: "POST",
            body: JSON.stringify(payload),
        }),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ["campaigns", "detail", id] });
            qc.invalidateQueries({ queryKey: ["campaigns", "dashboard", id] });
            qc.invalidateQueries({ queryKey: ["campaigns", "list"] });
        },
    });
}

export function useMyCampaigns() {
    return useQuery<EmployeeCampaign[]>({
        queryKey: ["campaigns", "me"],
        queryFn: () => apiFetch<EmployeeCampaign[]>("/api/campaigns/me"),
        staleTime: SOMETIMES,
    });
}
