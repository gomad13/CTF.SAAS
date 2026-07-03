"use client";

import { useState } from "react";
import { ArrowLeft } from "lucide-react";
import { apiFetch } from "@/lib/api";
import { useIsMobile } from "@/hooks/useMediaQuery";

// ── Types ─────────────────────────────────────────────────────────────────────

interface Email {
    id: string;
    from_name: string;
    from_address: string;
    subject: string;
    preview: string;
    sent_at: string;
    body: string;
}

interface MailboxContent {
    emails: Email[];
}

interface EmailDetail {
    id: string;
    fromName: string;
    subject: string;
    isDangerous: boolean;
    wasChecked: boolean;
    redFlags: string[];
}

interface SubmitResult {
    truePositives: number;
    falsePositives: number;
    missed: number;
    score: number;
    maxScore: number;
    emailDetails: EmailDetail[];
}

interface Props {
    challengeId: string;
    content: MailboxContent;
    variantIndex?: number | null;
    onComplete: (score?: number, maxScore?: number) => void;
}

// ── Avatar color (deterministic from name) ────────────────────────────────────

function avatarColor(name: string): string {
    const colors = ["#0e7490","#0d9488","var(--pr)","#b45309","#be123c","var(--pr-h)","#065f46","#9a3412"];
    let h = 0;
    for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) % colors.length;
    return colors[Math.abs(h)];
}

// ── Component ─────────────────────────────────────────────────────────────────

export default function MailboxChallenge({ challengeId, content, variantIndex = null, onComplete }: Props) {
    const [checked, setChecked]   = useState<Set<string>>(new Set());
    const [selected, setSelected] = useState<Email | null>(content.emails[0] ?? null);
    const [result, setResult]     = useState<SubmitResult | null>(null);
    const [loading, setLoading]   = useState(false);
    const [error, setError]       = useState<string | null>(null);
    const [view, setView]         = useState<"list" | "email">("list");
    const isMobile = useIsMobile();

    function openEmail(email: Email) {
        setSelected(email);
        setView("email");
    }

    function toggleCheck(id: string, e: React.MouseEvent) {
        e.stopPropagation();
        setChecked(prev => {
            const next = new Set(prev);
            next.has(id) ? next.delete(id) : next.add(id);
            return next;
        });
    }

    async function handleSubmit() {
        setError(null);
        setLoading(true);
        try {
            const res = await apiFetch<SubmitResult>(
                `/api/challenges/interactive/${challengeId}/submit-mailbox`,
                { method: "POST", body: JSON.stringify({ checkedEmailIds: [...checked], variantIndex }) }
            );
            setResult(res);
            setView("list");
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : "Erreur lors de l'évaluation.");
        } finally {
            setLoading(false);
        }
    }

    // ── Corrected view ──────────────────────────────────────────────────────────
    if (result) {
        const detailMap = new Map(result.emailDetails.map(e => [e.id, e]));
        const currentDetail = selected ? detailMap.get(selected.id) : null;

        const showList  = !isMobile || view === "list";
        const showEmail = !isMobile || view === "email";

        return (
            <div className="space-y-5">
                {/* Score row */}
                <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(90px, 1fr))", gap: 10 }}>
                    {[
                        { label: "Détectés",     value: result.truePositives,  color: "#10B981", bg: "rgba(34,197,94,0.1)" },
                        { label: "Faux positifs", value: result.falsePositives, color: "#ef4444", bg: "rgba(239,68,68,0.1)" },
                        { label: "Manqués",       value: result.missed,         color: "#f97316", bg: "rgba(249,115,22,0.1)" },
                    ].map(t => (
                        <div key={t.label} style={{ background: t.bg, border: `1px solid ${t.color}33`, borderRadius: 8, padding: "12px 8px", textAlign: "center" }}>
                            <p style={{ fontSize: 26, fontWeight: 700, color: t.color, lineHeight: 1 }}>{t.value}</p>
                            <p style={{ fontSize: 11, color: "var(--text-2)", marginTop: 4 }}>{t.label}</p>
                        </div>
                    ))}
                </div>

                {/* Score */}
                <div style={{ background: "var(--accent-subtle)", border: "1px solid var(--accent-border)", borderRadius: 10, padding: "16px", textAlign: "center" }}>
                    <p style={{ color: "var(--text-2)", fontSize: 12 }}>Score final</p>
                    <p style={{ color: "var(--pr)", fontSize: 36, fontWeight: 700, lineHeight: 1.1 }}>{result.score}</p>
                    <p style={{ color: "var(--text-2)", fontSize: 12 }}>/ {result.maxScore} pts</p>
                </div>

                {/* Email list + detail */}
                <div style={{ display: "flex", gap: 0, border: "1px solid var(--accent-border)", borderRadius: 10, overflow: "hidden", minHeight: 300 }}>
                    {/* Left list */}
                    {showList && (
                    <div style={{ width: isMobile ? "100%" : "40%", borderRight: isMobile ? "none" : "1px solid var(--border)", overflowY: "auto", background: "var(--bg-card)" }}>
                        <div style={{ background: "var(--bg-surface)", padding: "10px 14px", borderBottom: "1px solid var(--border)" }}>
                            <p style={{ color: "var(--text-2)", fontSize: 11, fontWeight: 600, textTransform: "uppercase", letterSpacing: "0.06em" }}>Boîte de réception</p>
                        </div>
                        {content.emails.map(email => {
                            const detail = detailMap.get(email.id);
                            const badge = detail?.isDangerous && detail?.wasChecked   ? { icon: "✅", color: "#10B981" }
                                        : detail?.isDangerous && !detail?.wasChecked  ? { icon: "❌", color: "#ef4444" }
                                        : !detail?.isDangerous && detail?.wasChecked  ? { icon: "⚠️", color: "#f97316" }
                                        : { icon: "✓",  color: "var(--text-2)" };
                            return (
                                <button
                                    key={email.id}
                                    onClick={() => openEmail(email)}
                                    style={{
                                        width: "100%", textAlign: "left", padding: "10px 14px", minHeight: 44,
                                        background: selected?.id === email.id ? "var(--accent-subtle)" : "transparent",
                                        borderLeft: selected?.id === email.id ? "3px solid var(--accent)" : "3px solid transparent",
                                        borderBottom: "1px solid var(--border)",
                                        cursor: "pointer", display: "flex", alignItems: "center", gap: 8,
                                    }}
                                >
                                    <span style={{ fontSize: 16, flexShrink: 0 }}>{badge.icon}</span>
                                    <div style={{ minWidth: 0, flex: 1 }}>
                                        <p style={{ fontSize: 12, fontWeight: 600, color: "var(--text)", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{email.from_name}</p>
                                        <p style={{ fontSize: 11, color: "var(--text-2)", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{email.subject}</p>
                                    </div>
                                </button>
                            );
                        })}
                    </div>
                    )}
                    {/* Right detail */}
                    {showEmail && (
                    <div style={{ flex: 1, minWidth: 0, background: "var(--bg-card)", padding: "16px", overflowY: "auto" }}>
                        {isMobile && (
                            <button
                                onClick={() => setView("list")}
                                style={{ display: "inline-flex", alignItems: "center", gap: 6, minHeight: 44, padding: "8px 4px", marginBottom: 8, background: "none", border: "none", color: "var(--pr)", fontSize: 13, fontWeight: 600, cursor: "pointer" }}
                            >
                                <ArrowLeft size={16} /> Retour à la liste
                            </button>
                        )}
                        {selected ? (
                            <>
                                <p style={{ color: "var(--text)", fontWeight: 700, fontSize: 14, marginBottom: 8, overflowWrap: "break-word" }}>{selected.subject}</p>
                                <p style={{ fontSize: 12, color: "var(--text-2)", marginBottom: 2, overflowWrap: "break-word", wordBreak: "break-word" }}>De : <span style={{ color: "var(--text)" }}>{selected.from_name}</span> &lt;{selected.from_address}&gt;</p>
                                <p style={{ fontSize: 11, color: "var(--text-2)", marginBottom: 12 }}>{selected.sent_at}</p>
                                <pre style={{ whiteSpace: "pre-wrap", overflowWrap: "break-word", wordBreak: "break-word", fontFamily: "inherit", fontSize: 13, lineHeight: 1.7, color: "var(--text-2)", margin: 0 }}>{selected.body}</pre>
                                {/* Red flags for dangerous selected email */}
                                {currentDetail?.isDangerous && currentDetail.redFlags.length > 0 && (
                                    <div style={{ marginTop: 14, background: "var(--danger-subtle)", border: "1px solid rgba(239,68,68,0.3)", borderRadius: 8, padding: "12px 14px" }}>
                                        <p style={{ color: "var(--danger-t)", fontSize: 12, fontWeight: 600, marginBottom: 8 }}>Éléments suspects dans cet email :</p>
                                        <ul style={{ padding: 0, margin: 0, listStyle: "none", display: "flex", flexDirection: "column", gap: 4 }}>
                                            {currentDetail.redFlags.map((f, i) => (
                                                <li key={i} style={{ fontSize: 12, color: "var(--danger-t)", display: "flex", gap: 6 }}>
                                                    <span>•</span><span>{f}</span>
                                                </li>
                                            ))}
                                        </ul>
                                    </div>
                                )}
                            </>
                        ) : (
                            <p style={{ color: "var(--text-2)", fontSize: 13, textAlign: "center", marginTop: 40 }}>Sélectionnez un email pour le lire</p>
                        )}
                    </div>
                    )}
                </div>

                <button
                    onClick={() => onComplete(result.score, result.maxScore)}
                    style={{ width: "100%", background: "var(--pr)", color: "#FFFFFF", border: "none", borderRadius: 8, padding: "13px 0", fontSize: 14, fontWeight: 600, cursor: "pointer" }}
                    onMouseOver={e => (e.currentTarget.style.background = "var(--pr-h)")}
                    onMouseOut={e => (e.currentTarget.style.background = "var(--pr)")}
                >
                    Continuer →
                </button>
            </div>
        );
    }

    // ── Reading view ────────────────────────────────────────────────────────────
    const showList  = !isMobile || view === "list";
    const showEmail = !isMobile || view === "email";

    return (
        <div className="space-y-4">
            {/* 2-column email client (single column on mobile with list/email switch) */}
            <div style={{ display: "flex", border: "1px solid var(--accent-border)", borderRadius: 10, overflow: "hidden", minHeight: 380 }}>
                {/* Left — list */}
                {showList && (
                <div style={{ width: isMobile ? "100%" : "40%", borderRight: isMobile ? "none" : "1px solid var(--border)", background: "var(--bg-card)", display: "flex", flexDirection: "column" }}>
                    <div style={{ background: "var(--bg-surface)", padding: "10px 14px", borderBottom: "1px solid var(--border)" }}>
                        <p style={{ color: "var(--text-2)", fontSize: 11, fontWeight: 600 }}>
                            📬 Boîte de réception ({content.emails.length})
                        </p>
                    </div>
                    <div style={{ overflowY: "auto", flex: 1 }}>
                        {content.emails.map(email => (
                            <div
                                key={email.id}
                                onClick={() => openEmail(email)}
                                style={{
                                    padding: "10px 12px", minHeight: 44,
                                    borderLeft: selected?.id === email.id ? "3px solid var(--accent)" : "3px solid transparent",
                                    borderBottom: "1px solid var(--border)",
                                    background: selected?.id === email.id ? "var(--accent-subtle)" : "transparent",
                                    cursor: "pointer", display: "flex", alignItems: "flex-start", gap: 8,
                                }}
                            >
                                {/* Checkbox — cible tactile agrandie (mobile-friendly) */}
                                <span
                                    onClick={e => toggleCheck(email.id, e)}
                                    role="checkbox"
                                    aria-checked={checked.has(email.id)}
                                    aria-label="Marquer comme suspect"
                                    title="Marquer comme suspect"
                                    style={{
                                        display: "inline-flex", alignItems: "center", justifyContent: "center",
                                        width: 26, height: 26, borderRadius: 6, border: checked.has(email.id) ? "none" : "2px solid var(--text-3)",
                                        background: checked.has(email.id) ? "var(--pr)" : "transparent",
                                        flexShrink: 0, marginTop: 1, cursor: "pointer", padding: 9, boxSizing: "content-box",
                                    }}
                                >
                                    {checked.has(email.id) && <svg width="15" height="15" viewBox="0 0 10 10"><path d="M2 5l2.5 2.5L8 3" stroke="#fff" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" fill="none"/></svg>}
                                </span>
                                {/* Avatar */}
                                <span style={{ width: 28, height: 28, borderRadius: "50%", background: avatarColor(email.from_name), display: "flex", alignItems: "center", justifyContent: "center", color: "var(--text)", fontSize: 11, fontWeight: 700, flexShrink: 0 }}>
                                    {email.from_name.charAt(0).toUpperCase()}
                                </span>
                                <div style={{ minWidth: 0, flex: 1 }}>
                                    <div style={{ display: "flex", justifyContent: "space-between", gap: 4 }}>
                                        <p style={{ fontSize: 12, fontWeight: 700, color: "var(--text)", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{email.from_name}</p>
                                        <p style={{ fontSize: 10, color: "var(--text-2)", whiteSpace: "nowrap", flexShrink: 0 }}>{email.sent_at}</p>
                                    </div>
                                    <p style={{ fontSize: 11, color: "var(--text-2)", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap", marginTop: 1 }}>{email.subject}</p>
                                    <p style={{ fontSize: 10, color: "var(--text-2)", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap", marginTop: 1 }}>{email.preview}</p>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
                )}

                {/* Right — detail */}
                {showEmail && (
                <div style={{ flex: 1, minWidth: 0, background: "var(--bg-card)", padding: "16px 18px", overflowY: "auto" }}>
                    {isMobile && (
                        <button
                            onClick={() => setView("list")}
                            style={{ display: "inline-flex", alignItems: "center", gap: 6, minHeight: 44, padding: "8px 4px", marginBottom: 8, background: "none", border: "none", color: "var(--pr)", fontSize: 13, fontWeight: 600, cursor: "pointer" }}
                        >
                            <ArrowLeft size={16} /> Retour à la liste
                        </button>
                    )}
                    {selected ? (
                        <>
                            <p style={{ color: "var(--text)", fontWeight: 700, fontSize: 15, marginBottom: 10, overflowWrap: "break-word" }}>{selected.subject}</p>
                            <div style={{ borderBottom: "1px solid var(--border)", paddingBottom: 10, marginBottom: 12 }}>
                                <p style={{ fontSize: 12, color: "var(--text-2)", overflowWrap: "break-word" }}>De : <span style={{ color: "var(--text)" }}>{selected.from_name}</span> <span style={{ fontFamily: "monospace", wordBreak: "break-all" }}>&lt;{selected.from_address}&gt;</span></p>
                                <p style={{ fontSize: 12, color: "var(--text-2)", marginTop: 3 }}>À : vous@email.fr</p>
                                <p style={{ fontSize: 11, color: "var(--text-2)", marginTop: 3 }}>{selected.sent_at}</p>
                            </div>
                            <pre style={{ whiteSpace: "pre-wrap", overflowWrap: "break-word", wordBreak: "break-word", fontFamily: "inherit", fontSize: 14, lineHeight: 1.7, color: "var(--text-2)", margin: 0 }}>
                                {selected.body.split(/(https?:\/\/[^\s]+)/g).map((part, i) =>
                                    /^https?:\/\//.test(part)
                                        ? <span key={i} style={{ color: "var(--pr)", cursor: "default" }}>{part}</span>
                                        : part
                                )}
                            </pre>
                        </>
                    ) : (
                        <p style={{ color: "var(--text-2)", fontSize: 13, textAlign: "center", marginTop: 60 }}>Sélectionnez un email pour le lire</p>
                    )}
                </div>
                )}
            </div>

            {/* Footer */}
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12, flexWrap: "wrap" }}>
                <p style={{ fontSize: 13, color: "var(--text-2)" }}>
                    <span style={{ color: "var(--pr)", fontWeight: 600 }}>{checked.size}</span> email(s) marqué(s) comme dangereux
                </p>
                {error && <p style={{ color: "var(--danger-t)", fontSize: 13 }}>{error}</p>}
                <button
                    onClick={handleSubmit}
                    disabled={loading}
                    style={{
                        background: loading ? "var(--accent-subtle)" : "var(--pr)",
                        color: "#FFFFFF", border: "none", borderRadius: 8,
                        padding: "10px 22px", fontSize: 13, fontWeight: 600,
                        cursor: loading ? "not-allowed" : "pointer",
                    }}
                    onMouseOver={e => { if (!loading) e.currentTarget.style.background = "var(--pr-h)"; }}
                    onMouseOut={e => { if (!loading) e.currentTarget.style.background = "var(--pr)"; }}
                >
                    {loading ? "Analyse…" : "Soumettre mon analyse"}
                </button>
            </div>
        </div>
    );
}
