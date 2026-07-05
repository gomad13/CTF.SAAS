"use client";

import { useCallback, useMemo, useState } from "react";
import { apiFetch } from "@/lib/api";
import type { ConsentItem, MissingConsent } from "@/lib/types/legal";

type Props = {
    missing: MissingConsent[];
    onAccepted: () => void;
};

/**
 * Modal bloquante (overlay non fermable) déclenchée quand un user authentifié
 * n'a pas accepté la dernière version d'un document requis. POST en série
 * vers /api/me/consents/re-accept avec les versions courantes acceptées.
 */
export default function ConsentUpdateModal({ missing, onAccepted }: Props) {
    const [accepted, setAccepted] = useState<Record<string, boolean>>({});
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const allChecked = useMemo(
        () => missing.every(m => accepted[m.documentSlug]),
        [missing, accepted],
    );

    const toggle = useCallback((slug: string, value: boolean) => {
        setAccepted(prev => ({ ...prev, [slug]: value }));
    }, []);

    const handleSubmit = useCallback(async () => {
        if (!allChecked || submitting) return;
        setSubmitting(true);
        setError(null);
        try {
            const consents: ConsentItem[] = missing.map(m => ({
                documentSlug: m.documentSlug,
                documentVersion: m.currentVersion,
                accepted: true,
            }));
            await apiFetch("/api/me/consents/re-accept", {
                method: "POST",
                body: JSON.stringify({ consents }),
            });
            onAccepted();
        } catch (e) {
            setError(e instanceof Error ? e.message : "Erreur lors de l'enregistrement.");
        } finally {
            setSubmitting(false);
        }
    }, [allChecked, submitting, missing, onAccepted]);

    return (
        <div
            role="dialog"
            aria-modal="true"
            aria-labelledby="consent-update-title"
            style={{
                position: "fixed", inset: 0, zIndex: 9999,
                background: "rgba(0, 0, 0, 0.7)",
                backdropFilter: "blur(8px)",
                display: "flex", alignItems: "center", justifyContent: "center",
                padding: 16,
            }}
        >
            <div style={{
                background: "var(--surface)", color: "var(--text)",
                borderRadius: 12, padding: "clamp(20px, 5vw, 28px)", maxWidth: 640, width: "100%",
                maxHeight: "90vh", overflowY: "auto",
                boxShadow: "0 20px 50px -10px rgba(0,0,0,0.4)",
                borderTop: "4px solid var(--accent)",
            }}>
                <h2 id="consent-update-title" style={{ fontSize: 22, fontWeight: 700, margin: "0 0 8px" }}>
                    Mise à jour de nos conditions
                </h2>
                <p style={{ fontSize: 14, lineHeight: 1.6, color: "var(--text-2)", margin: "0 0 20px" }}>
                    Pour continuer à utiliser Sentys, merci d&apos;accepter la nouvelle version
                    {missing.length > 1 ? " des documents suivants" : " du document suivant"}.
                </p>

                <div style={{ display: "flex", flexDirection: "column", gap: 12, marginBottom: 16 }}>
                    {missing.map(m => (
                        <div key={m.documentSlug} style={{
                            border: "1px solid var(--border)", borderRadius: 10, padding: 16,
                            background: "var(--surface-2)",
                        }}>
                            <div style={{ display: "flex", flexWrap: "wrap", alignItems: "flex-start", justifyContent: "space-between", gap: 8, marginBottom: 8 }}>
                                <div style={{ minWidth: 0 }}>
                                    <div style={{ fontSize: 15, fontWeight: 600, color: "var(--text)" }}>{m.documentTitle}</div>
                                    <div style={{ fontSize: 12, color: "var(--text-3)", marginTop: 2 }}>
                                        {m.lastAcceptedVersion
                                            ? <>Version acceptée : <strong>{m.lastAcceptedVersion}</strong> → nouvelle : <strong>{m.currentVersion}</strong></>
                                            : <>Nouvelle version : <strong>{m.currentVersion}</strong></>
                                        }
                                    </div>
                                </div>
                                <a
                                    href={`/legal/${m.documentSlug}`}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="transition-colors duration-200 hover:text-[var(--accent-hover)]"
                                    style={{ fontSize: 12, color: "var(--accent)", whiteSpace: "nowrap", textDecoration: "underline" }}
                                >
                                    Lire la nouvelle version
                                </a>
                            </div>
                            {m.changeLog && (
                                <div style={{
                                    fontSize: 12, color: "var(--text-2)",
                                    background: "var(--surface)", border: "1px solid var(--border)",
                                    borderRadius: 6, padding: "8px 10px", marginBottom: 10,
                                }}>
                                    <strong>Changements&nbsp;:</strong> {m.changeLog}
                                </div>
                            )}
                            <label style={{ display: "flex", alignItems: "center", gap: 10, cursor: "pointer", fontSize: 13, minHeight: 44 }}>
                                <input
                                    type="checkbox"
                                    checked={!!accepted[m.documentSlug]}
                                    onChange={e => toggle(m.documentSlug, e.target.checked)}
                                    style={{ accentColor: "var(--accent)", width: 20, height: 20, flexShrink: 0 }}
                                />
                                <span>J&apos;accepte la version {m.currentVersion} de {m.documentTitle}</span>
                            </label>
                        </div>
                    ))}
                </div>

                {error && (
                    <div role="alert" style={{
                        background: "var(--danger-subtle)", border: "1px solid var(--danger-subtle)",
                        color: "var(--danger-t)", padding: "10px 12px", borderRadius: 8, fontSize: 13, marginBottom: 12,
                    }}>{error}</div>
                )}

                <button
                    type="button"
                    onClick={handleSubmit}
                    disabled={!allChecked || submitting}
                    style={{
                        width: "100%",
                        padding: "12px 20px",
                        minHeight: 44,
                        background: allChecked && !submitting ? "var(--accent)" : "var(--text-3)",
                        color: "var(--on-accent)",
                        border: "none",
                        borderRadius: 10,
                        fontSize: 14,
                        fontWeight: 600,
                        cursor: allChecked && !submitting ? "pointer" : "not-allowed",
                        transition: "background 0.2s",
                    }}
                    onMouseEnter={e => { if (allChecked && !submitting) (e.currentTarget as HTMLButtonElement).style.background = "var(--accent-hover)"; }}
                    onMouseLeave={e => { if (allChecked && !submitting) (e.currentTarget as HTMLButtonElement).style.background = "var(--accent)"; }}
                >
                    {submitting ? "Enregistrement…" : "Accepter et continuer"}
                </button>
            </div>
        </div>
    );
}
