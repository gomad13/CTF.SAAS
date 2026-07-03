"use client";

import { useState } from "react";
import { apiFetch } from "@/lib/api";

// ── Types ─────────────────────────────────────────────────────────────────────

interface PhishingEmail {
    from_name: string;
    from_address: string;
    to: string;
    subject: string;
    sent_at: string;
    body: string;
}

interface PhishingContent {
    email: PhishingEmail;
    instructions: string;
}

interface AiAnalysis {
    score: number;
    elements_trouves: string[];
    elements_manques: string[];
    erreurs: string[];
    resume: string;
    conseil: string;
}

interface SubmitResult {
    score: number;
    maxScore: number;
    analysis: AiAnalysis;
}

interface Props {
    challengeId: string;
    content: PhishingContent;
    onComplete: (score?: number, maxScore?: number) => void;
}

type ViewState = "reading" | "analyzing" | "corrected";

// ── Score circle SVG ──────────────────────────────────────────────────────────

function ScoreCircle({ pct }: { pct: number }) {
    const r = 58;
    const circ = 2 * Math.PI * r;
    const dash = (pct / 100) * circ;
    const color = pct >= 90 ? "var(--pr)" : pct >= 75 ? "var(--success)" : pct >= 50 ? "var(--warning)" : "var(--danger)";

    return (
        <svg width="150" height="150" viewBox="0 0 150 150">
            <circle cx="75" cy="75" r={r} fill="none" stroke="var(--border)" strokeWidth="10"/>
            <circle
                cx="75" cy="75" r={r}
                fill="none" stroke={color} strokeWidth="10"
                strokeDasharray={`${dash} ${circ - dash}`}
                strokeLinecap="round"
                transform="rotate(-90 75 75)"
            />
            <text x="75" y="70" textAnchor="middle" fill={color} fontSize="28" fontWeight="700" fontFamily="inherit">{pct}</text>
            <text x="75" y="90" textAnchor="middle" fill="var(--text-2)" fontSize="13" fontFamily="inherit">/100</text>
        </svg>
    );
}

// ── Component ─────────────────────────────────────────────────────────────────

export default function PhishingAiChallenge({ challengeId, content, onComplete }: Props) {
    const [text, setText]       = useState("");
    const [view, setView]       = useState<ViewState>("reading");
    const [result, setResult]   = useState<SubmitResult | null>(null);
    const [error, setError]     = useState<string | null>(null);

    async function handleSubmit() {
        if (text.trim().length < 15) {
            setError("Rédigez au moins 50 caractères pour une analyse valide.");
            return;
        }
        setError(null);
        setView("analyzing");
        try {
            const res = await apiFetch<SubmitResult>(
                `/api/challenges/interactive/${challengeId}/submit-phishing-ai`,
                { method: "POST", body: JSON.stringify({ userAnalysis: text.trim() }) }
            );
            setResult(res);
            setView("corrected");
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : "Erreur lors de l'analyse IA.");
            setView("reading");
        }
    }

    // ── Analyzing spinner ───────────────────────────────────────────────────────
    if (view === "analyzing") {
        return (
            <div style={{ display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", padding: "60px 20px", gap: 16 }}>
                <svg width="48" height="48" viewBox="0 0 48 48">
                    <circle cx="24" cy="24" r="20" fill="none" stroke="var(--border)" strokeWidth="4"/>
                    <circle cx="24" cy="24" r="20" fill="none" stroke="var(--pr)" strokeWidth="4"
                        strokeDasharray="60 66" strokeLinecap="round"
                        style={{ transformOrigin: "center", animation: "spin 1s linear infinite" }}
                    />
                    <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
                </svg>
                <p style={{ color: "var(--text)", fontSize: 15, fontWeight: 600 }}>L&apos;IA analyse votre réponse…</p>
                <p style={{ color: "var(--text-2)", fontSize: 13 }}>Cela peut prendre quelques secondes</p>
            </div>
        );
    }

    // ── Corrected view ──────────────────────────────────────────────────────────
    if (view === "corrected" && result) {
        const pct = result.analysis.score;

        return (
            <div className="space-y-5">
                {/* Score + resume */}
                <div style={{ display: "flex", alignItems: "center", gap: 20, flexWrap: "wrap", justifyContent: "center", background: "var(--bg-card)", border: "1px solid var(--accent-border)", borderRadius: 10, padding: "16px 20px" }}>
                    <ScoreCircle pct={pct} />
                    <div style={{ flex: 1, minWidth: 0 }}>
                        <p style={{ color: "var(--text-2)", fontSize: 12, marginBottom: 4 }}>Score IA</p>
                        <p style={{ color: "var(--pr)", fontSize: 28, fontWeight: 700, lineHeight: 1 }}>{result.score} pts</p>
                        <p style={{ color: "var(--text-2)", fontSize: 12, marginBottom: 10 }}>/ {result.maxScore} pts</p>
                        <p style={{ color: "var(--text-2)", fontSize: 13, fontStyle: "italic", lineHeight: 1.5 }}>&ldquo;{result.analysis.resume}&rdquo;</p>
                    </div>
                </div>

                {/* Elements trouvés */}
                {result.analysis.elements_trouves?.length > 0 && (
                    <div style={{ background: "rgba(34,197,94,0.08)", borderLeft: "3px solid var(--success)", borderRadius: "0 6px 6px 0", padding: "14px 16px" }}>
                        <p style={{ color: "var(--success)", fontSize: 13, fontWeight: 600, marginBottom: 8 }}>Ce que vous avez bien repéré</p>
                        <ul style={{ padding: 0, margin: 0, listStyle: "none", display: "flex", flexDirection: "column", gap: 5 }}>
                            {result.analysis.elements_trouves.map((item, i) => (
                                <li key={i} style={{ fontSize: 13, color: "var(--success-t)", display: "flex", gap: 8 }}>
                                    <span style={{ color: "var(--success)" }}>✓</span><span>{item}</span>
                                </li>
                            ))}
                        </ul>
                    </div>
                )}

                {/* Elements manqués */}
                {result.analysis.elements_manques?.length > 0 && (
                    <div style={{ background: "var(--warning-subtle)", borderLeft: "3px solid var(--warning)", borderRadius: "0 6px 6px 0", padding: "14px 16px" }}>
                        <p style={{ color: "var(--warning)", fontSize: 13, fontWeight: 600, marginBottom: 8 }}>Ce que vous avez manqué</p>
                        <ul style={{ padding: 0, margin: 0, listStyle: "none", display: "flex", flexDirection: "column", gap: 5 }}>
                            {result.analysis.elements_manques.map((item, i) => (
                                <li key={i} style={{ fontSize: 13, color: "var(--warning-t)", display: "flex", gap: 8 }}>
                                    <span style={{ color: "var(--warning)" }}>→</span><span>{item}</span>
                                </li>
                            ))}
                        </ul>
                    </div>
                )}

                {/* Erreurs */}
                {result.analysis.erreurs?.length > 0 && (
                    <div style={{ background: "var(--danger-subtle)", borderLeft: "3px solid var(--danger)", borderRadius: "0 6px 6px 0", padding: "14px 16px" }}>
                        <p style={{ color: "var(--danger)", fontSize: 13, fontWeight: 600, marginBottom: 8 }}>Erreurs à éviter</p>
                        <ul style={{ padding: 0, margin: 0, listStyle: "none", display: "flex", flexDirection: "column", gap: 5 }}>
                            {result.analysis.erreurs.map((item, i) => (
                                <li key={i} style={{ fontSize: 13, color: "var(--danger-t)", display: "flex", gap: 8 }}>
                                    <span style={{ color: "var(--danger)" }}>✗</span><span>{item}</span>
                                </li>
                            ))}
                        </ul>
                    </div>
                )}

                {/* Conseil */}
                {result.analysis.conseil && (
                    <div style={{ background: "var(--bg-surface)", border: "1px solid var(--accent-border)", borderRadius: 8, padding: "14px 16px" }}>
                        <p style={{ color: "var(--pr)", fontSize: 13, fontWeight: 600, marginBottom: 6 }}>💡 Conseil de l&apos;expert</p>
                        <p style={{ color: "var(--text-2)", fontSize: 13, lineHeight: 1.6 }}>{result.analysis.conseil}</p>
                    </div>
                )}

                <button
                    onClick={() => onComplete(result.score, result.maxScore)}
                    style={{ width: "100%", background: "var(--pr)", color: "#FFFFFF", border: "none", borderRadius: 8, padding: "13px 0", fontSize: 14, fontWeight: 600, cursor: "pointer" }}
                    onMouseOver={e => (e.currentTarget.style.background = "var(--pr-h)")}
                    onMouseOut={e => (e.currentTarget.style.background = "var(--pr)")}
                >
                    Terminer le parcours →
                </button>
            </div>
        );
    }

    // ── Reading view ────────────────────────────────────────────────────────────
    return (
        <div className="space-y-5">
            {/* Email card */}
            <div style={{ background: "var(--bg-card)", border: "1px solid var(--accent-border)", borderRadius: 10, overflow: "hidden" }}>
                <div style={{ background: "var(--bg-surface)", borderBottom: "1px solid var(--accent-border)", padding: "14px 18px" }}>
                    <div style={{ display: "flex", justifyContent: "space-between", gap: 12, alignItems: "flex-start" }}>
                        <div style={{ minWidth: 0 }}>
                            <p style={{ color: "var(--text)", fontWeight: 600, fontSize: 14 }}>{content.email.from_name}</p>
                            <p style={{ color: "var(--danger)", fontSize: 12, fontFamily: "monospace", marginTop: 2, wordBreak: "break-all" }}>{content.email.from_address}</p>
                        </div>
                        <span style={{ color: "var(--text-2)", fontSize: 11, whiteSpace: "nowrap", flexShrink: 0 }}>{content.email.sent_at}</span>
                    </div>
                    <p style={{ color: "var(--text)", fontWeight: 600, fontSize: 14, marginTop: 10 }}>{content.email.subject}</p>
                    <p style={{ color: "var(--text-2)", fontSize: 12, marginTop: 2 }}>À : {content.email.to}</p>
                </div>
                <div style={{ padding: "16px 18px" }}>
                    <div style={{ fontSize: 14, lineHeight: 1.7, color: "var(--text-2)", overflowWrap: "break-word", wordBreak: "break-word" }}>
                        {content.email.body.split("\n").map((line, i) => {
                            const urlMatch = line.match(/(https?:\/\/[^\s]+)/);
                            const bulletMatch = line.match(/^[•·-]\s(.+)/);
                            if (urlMatch) return (
                                <p key={i} style={{ margin: "4px 0" }}>
                                    {line.replace(urlMatch[1], "")}
                                    <span style={{ color: "var(--pr)", fontFamily: "monospace", fontSize: 13 }}>{urlMatch[1]}</span>
                                </p>
                            );
                            if (bulletMatch) return <p key={i} style={{ margin: "2px 0", paddingLeft: 12 }}>• {bulletMatch[1]}</p>;
                            return <p key={i} style={{ margin: line.trim() === "" ? "8px 0" : "4px 0" }}>{line}</p>;
                        })}
                    </div>
                </div>
            </div>

            {/* Instructions */}
            <div style={{ background: "var(--accent-subtle)", border: "1px solid var(--accent-border)", borderRadius: 8, padding: "12px 16px" }}>
                <p style={{ color: "var(--text-2)", fontSize: 13, lineHeight: 1.6 }}>
                    {content.instructions || "Analysez attentivement cet email et décrivez tout ce qui vous semble anormal, suspect ou dangereux. Soyez précis."}
                </p>
            </div>

            {/* Textarea */}
            <div>
                <label style={{ display: "block", color: "var(--text)", fontSize: 13, fontWeight: 600, marginBottom: 8 }}>
                    Votre analyse
                </label>
                <textarea
                    value={text}
                    onChange={e => setText(e.target.value)}
                    rows={6}
                    placeholder="Ex: L'adresse email de l'expéditeur n'est pas le vrai domaine de la Société Générale, le lien pointe vers un site inconnu..."
                    style={{
                        width: "100%", background: "var(--bg-card)", color: "var(--text)",
                        border: `1px solid ${text.length >= 15 ? "var(--pr)" : "var(--accent-border)"}`,
                        borderRadius: 8, padding: "12px 14px", fontSize: 14, lineHeight: 1.6,
                        minHeight: 160, resize: "vertical", outline: "none",
                        fontFamily: "inherit", boxSizing: "border-box",
                        transition: "border-color 0.15s",
                    }}
                    onFocus={e => { e.target.style.borderColor = "var(--pr)"; }}
                    onBlur={e => { e.target.style.borderColor = text.length >= 15 ? "var(--pr)" : "var(--accent-border)"; }}
                />
                <div style={{ display: "flex", justifyContent: "flex-end", marginTop: 4 }}>
                    <span style={{ fontSize: 11, color: text.length >= 15 ? "var(--pr)" : "var(--text-2)" }}>
                        {text.length} / 50 min
                    </span>
                </div>
            </div>

            {error && <p style={{ color: "var(--danger-t)", fontSize: 13, textAlign: "center" }}>{error}</p>}

            <button
                onClick={handleSubmit}
                disabled={text.trim().length < 15}
                style={{
                    width: "100%",
                    background: text.trim().length < 15 ? "var(--accent-subtle)" : "var(--pr)",
                    color: "#FFFFFF", border: "none", borderRadius: 8,
                    padding: "13px 0", fontSize: 14, fontWeight: 600,
                    cursor: text.trim().length < 15 ? "not-allowed" : "pointer",
                    transition: "background 0.15s",
                }}
                onMouseOver={e => { if (text.trim().length >= 15) e.currentTarget.style.background = "var(--pr-h)"; }}
                onMouseOut={e => { if (text.trim().length >= 15) e.currentTarget.style.background = "var(--pr)"; }}
            >
                Soumettre à l&apos;analyse IA
            </button>
        </div>
    );
}
