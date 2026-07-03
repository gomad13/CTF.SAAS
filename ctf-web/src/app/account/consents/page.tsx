"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import { RequireAuth } from "@/components/RequireAuth";
import type { UserConsent } from "@/lib/types/legal";

function formatDate(iso: string) {
    try {
        return new Date(iso).toLocaleString("fr-FR", {
            day: "2-digit", month: "short", year: "numeric",
            hour: "2-digit", minute: "2-digit",
        });
    } catch {
        return iso;
    }
}

function StatusPill({ isCurrent, accepted }: { isCurrent: boolean; accepted: boolean }) {
    if (!accepted) {
        return <span style={{
            display: "inline-flex", alignItems: "center", padding: "2px 10px",
            background: "rgba(239,68,68,0.10)", color: "#EF4444",
            borderRadius: 999, fontSize: 11, fontWeight: 600,
        }}>Retrait</span>;
    }
    if (isCurrent) {
        return <span style={{
            display: "inline-flex", alignItems: "center", padding: "2px 10px",
            background: "rgba(16,185,129,0.10)", color: "#10b981",
            borderRadius: 999, fontSize: 11, fontWeight: 600,
        }}>À jour</span>;
    }
    return <span style={{
        display: "inline-flex", alignItems: "center", padding: "2px 10px",
        background: "rgba(245,158,11,0.10)", color: "#F59E0B",
        borderRadius: 999, fontSize: 11, fontWeight: 600,
    }}>Mise à jour disponible</span>;
}

function ConsentsContent() {
    const { data, isLoading, error } = useQuery<UserConsent[]>({
        queryKey: ["legal", "my-consents"],
        queryFn: () => apiFetch<UserConsent[]>("/api/me/consents"),
        staleTime: 30 * 1000,
    });
    const items = data ?? [];
    const loading = isLoading;
    const errorMessage = error instanceof Error ? error.message : null;

    return (
        <main style={{ minHeight: "100svh", background: "#F8FAFC", padding: "32px 24px" }}>
            <div style={{ maxWidth: 900, margin: "0 auto" }}>
                <h1 style={{ fontSize: 26, fontWeight: 700, color: "#1E293B", margin: "0 0 6px" }}>Mes consentements</h1>
                <p style={{ fontSize: 14, color: "#64748B", margin: "0 0 24px" }}>
                    Historique de tous les consentements que vous avez donnés à Sentys, conformément à l&apos;article 7 du RGPD.
                </p>

                {loading && <div style={{ padding: 20, color: "#64748B" }}>Chargement…</div>}
                {errorMessage && <div style={{ padding: 16, background: "rgba(239,68,68,0.08)", color: "#B91C1C", borderRadius: 8 }}>{errorMessage}</div>}

                {!loading && !errorMessage && items.length === 0 && (
                    <div style={{
                        padding: 24, background: "#FFFFFF", border: "1px solid #E2E8F0",
                        borderRadius: 12, color: "#64748B", textAlign: "center", fontSize: 14,
                    }}>
                        Aucun consentement enregistré.
                    </div>
                )}

                {!loading && !errorMessage && items.length > 0 && (
                    <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
                        {items.map(c => (
                            <div key={c.id} style={{
                                background: "#FFFFFF", border: "1px solid #E2E8F0",
                                borderRadius: 12, padding: 16,
                            }}>
                                <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", gap: 12, marginBottom: 8 }}>
                                    <div>
                                        <div style={{ fontSize: 15, fontWeight: 600, color: "#1E293B" }}>{c.documentTitle}</div>
                                        <div style={{ fontSize: 12, color: "#64748B", marginTop: 2 }}>
                                            Version <strong>{c.documentVersion}</strong> — {formatDate(c.acceptedAt)} — {c.source}
                                        </div>
                                    </div>
                                    <StatusPill isCurrent={c.isCurrentVersion} accepted={c.accepted} />
                                </div>
                                <div style={{
                                    display: "grid", gridTemplateColumns: "1fr 1fr", gap: 8,
                                    marginTop: 8, padding: 10, background: "#F8FAFC",
                                    borderRadius: 8, fontSize: 11, color: "#475569",
                                }}>
                                    <div><strong>IP</strong> : {c.ipAddress ?? "—"}</div>
                                    <div><strong>UserAgent</strong> : {c.userAgent ? c.userAgent.slice(0, 80) + (c.userAgent.length > 80 ? "…" : "") : "—"}</div>
                                </div>
                                <div style={{ marginTop: 10 }}>
                                    <Link
                                        href={`/legal/${c.documentSlug}`}
                                        target="_blank"
                                        rel="noopener noreferrer"
                                        style={{ fontSize: 12, color: "var(--accent)", textDecoration: "underline" }}
                                    >
                                        Voir le document
                                    </Link>
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </main>
    );
}

export default function ConsentsPage() {
    return (
        <RequireAuth>
            <ConsentsContent />
        </RequireAuth>
    );
}
