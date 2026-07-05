"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import { RequireAuth } from "@/components/RequireAuth";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";
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
            background: "var(--danger-subtle)", color: "var(--danger-t)",
            borderRadius: 999, fontSize: 11, fontWeight: 600,
        }}>Retrait</span>;
    }
    if (isCurrent) {
        return <span style={{
            display: "inline-flex", alignItems: "center", padding: "2px 10px",
            background: "var(--success-subtle)", color: "var(--success-t)",
            borderRadius: 999, fontSize: 11, fontWeight: 600,
        }}>À jour</span>;
    }
    return <span style={{
        display: "inline-flex", alignItems: "center", padding: "2px 10px",
        background: "var(--warning-subtle)", color: "var(--warning-t)",
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
        <main style={{ minHeight: "100svh", background: "var(--bg)", padding: "32px 24px" }}>
            <div style={{ maxWidth: 900, margin: "0 auto" }}>
                <Reveal>
                    <h1 style={{ fontSize: 26, fontWeight: 700, color: "var(--text)", margin: "0 0 6px" }}>Mes consentements</h1>
                    <p style={{ fontSize: 14, color: "var(--text-2)", margin: "0 0 24px" }}>
                        Historique de tous les consentements que vous avez donnés à Sentys, conformément à l&apos;article 7 du RGPD.
                    </p>
                </Reveal>

                {loading && <div style={{ padding: 20, color: "var(--text-2)" }}>Chargement…</div>}
                {errorMessage && <div style={{ padding: 16, background: "var(--danger-subtle)", color: "var(--danger-t)", borderRadius: 8 }}>{errorMessage}</div>}

                {!loading && !errorMessage && items.length === 0 && (
                    <div style={{
                        padding: 24, background: "var(--surface)", border: "1px solid var(--border)",
                        borderRadius: 12, color: "var(--text-3)", textAlign: "center", fontSize: 14,
                    }}>
                        Aucun consentement enregistré.
                    </div>
                )}

                {!loading && !errorMessage && items.length > 0 && (
                    <Stagger className="flex flex-col gap-3">
                        {items.map(c => (
                            <StaggerItem key={c.id}>
                            <div className="transition-colors duration-200" style={{
                                background: "var(--surface)", border: "1px solid var(--border)",
                                borderRadius: 12, padding: 16,
                            }}
                                onMouseEnter={e => (e.currentTarget.style.borderColor = "var(--accent-border)")}
                                onMouseLeave={e => (e.currentTarget.style.borderColor = "var(--border)")}>
                                <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", gap: 12, marginBottom: 8 }}>
                                    <div>
                                        <div style={{ fontSize: 15, fontWeight: 600, color: "var(--text)" }}>{c.documentTitle}</div>
                                        <div style={{ fontSize: 12, color: "var(--text-2)", marginTop: 2 }}>
                                            Version <strong>{c.documentVersion}</strong> — {formatDate(c.acceptedAt)} — {c.source}
                                        </div>
                                    </div>
                                    <StatusPill isCurrent={c.isCurrentVersion} accepted={c.accepted} />
                                </div>
                                <div style={{
                                    display: "grid", gridTemplateColumns: "1fr 1fr", gap: 8,
                                    marginTop: 8, padding: 10, background: "var(--surface-2)",
                                    borderRadius: 8, fontSize: 11, color: "var(--text-2)",
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
                            </StaggerItem>
                        ))}
                    </Stagger>
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
