"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import type {
    InviteDto,
    CreatedInviteDto,
    CreateInviteRequest,
} from "@/lib/types/invites";

const KEY = ["admin", "invites"] as const;

/** Liste les invitations du tenant courant (admin). */
export function useInvites() {
    return useQuery<InviteDto[]>({
        queryKey: KEY,
        queryFn: () => apiFetch<InviteDto[]>("/api/admin/invites"),
        staleTime: 30 * 1000,
    });
}

/** Crée une invitation et renvoie le token/URL (une seule fois). */
export function useCreateInvite() {
    const qc = useQueryClient();
    return useMutation<CreatedInviteDto, Error, CreateInviteRequest>({
        mutationFn: (req) =>
            apiFetch<CreatedInviteDto>("/api/admin/invites", {
                method: "POST",
                body: JSON.stringify(req),
            }),
        onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
    });
}

/** Révoque une invitation. */
export function useRevokeInvite() {
    const qc = useQueryClient();
    return useMutation<void, Error, string>({
        mutationFn: (id) =>
            apiFetch<void>(`/api/admin/invites/${id}`, { method: "DELETE" }),
        onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
    });
}
