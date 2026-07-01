"use client";

import dynamic from "next/dynamic";
import { useQuery } from "@tanstack/react-query";
import { usePathname } from "next/navigation";
import { apiFetch } from "@/lib/api";
import type { ConsentStatus } from "@/lib/types/legal";

const ConsentUpdateModal = dynamic(() => import("./ConsentUpdateModal"), { ssr: false });

// Routes pour lesquelles on ne va PAS interroger /me/consents/status :
// - landing / login / register / pages légales / pages publiques de récupération
const PUBLIC_PATH_PREFIXES = [
    "/login",
    "/register",
    "/forgot-password",
    "/reset-password",
    "/legal",
    "/landing",
    "/cgu",
    "/privacy",
    "/mentions-legales",
];

function isPublicPath(p: string): boolean {
    if (p === "/") return true;
    return PUBLIC_PATH_PREFIXES.some(prefix => p === prefix || p.startsWith(prefix + "/"));
}

/**
 * Sentinelle globale : pour chaque page authentifiée, vérifie via
 * GET /api/me/consents/status si l'utilisateur a accepté la dernière
 * version des documents requis. Si non, affiche la modal bloquante.
 *
 * Pas de loader visible : si pas authentifié, le 401 est silencieux et
 * la modal ne s'affiche pas (l'app continue son flux normal).
 */
export default function ConsentGate() {
    const pathname = usePathname();
    const isPublic = !pathname || isPublicPath(pathname);

    const { data: status, refetch } = useQuery<ConsentStatus | null>({
        queryKey: ["legal", "consent-status", pathname],
        queryFn: async () => {
            try {
                return await apiFetch<ConsentStatus>("/api/me/consents/status");
            } catch {
                // 401 → user pas connecté, pas de modal à afficher
                return null;
            }
        },
        enabled: !isPublic,
        staleTime: 60 * 1000,
        retry: false,
    });

    if (isPublic || !status || status.isUpToDate || status.missingConsents.length === 0) return null;

    return (
        <ConsentUpdateModal
            missing={status.missingConsents}
            onAccepted={() => { void refetch(); }}
        />
    );
}
