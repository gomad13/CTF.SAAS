"use client";

import { memo, useState } from "react";
import { ChevronDown } from "lucide-react";
import type { CoachingFeedback } from "@/lib/types/coaching";

const C_PRIMARY = "var(--accent)";
const C_BG = "var(--surface-2)";
const C_ACCENT = "var(--accent)";

const TYPE_LABELS: Record<string, string> = {
    ceo_fraud: "Arnaque au président",
    mailbox: "Tri boîte mail",
    multichoice: "QCM",
    password_quiz: "Mots de passe",
    phishing_ai: "Phishing IA",
};

function formatDateFr(iso: string): string {
    try {
        const d = new Date(iso);
        const dateStr = d.toLocaleDateString("fr-FR", { day: "numeric", month: "long", year: "numeric" });
        const timeStr = d.toLocaleTimeString("fr-FR", { hour: "2-digit", minute: "2-digit" });
        return `${dateStr} à ${timeStr}`;
    } catch {
        return "—";
    }
}

function CoachingDetailCardImpl({ item }: { item: CoachingFeedback }) {
    const [expanded, setExpanded] = useState(false);
    const label = TYPE_LABELS[item.challengeType] ?? item.challengeType;

    return (
        <article
            className="rounded-xl p-5 transition-colors duration-200"
            style={{
                background: C_BG,
                borderLeft: `4px solid ${C_PRIMARY}`,
                color: "rgba(255,255,255,0.92)",
            }}
        >
            <header className="mb-3 flex items-start justify-between gap-3">
                <div>
                    <p className="text-xs font-medium uppercase tracking-wider" style={{ color: "rgba(255,255,255,0.55)" }}>
                        {formatDateFr(item.createdAt)}
                    </p>
                    {item.status === "Fallback" && (
                        <p className="mt-1 text-xs" style={{ color: "rgba(255,255,255,0.5)" }}>
                            Modèle générique
                        </p>
                    )}
                </div>
                <span
                    className="rounded-full px-2.5 py-0.5 text-xs font-medium"
                    style={{ background: `${C_ACCENT}22`, color: C_ACCENT }}
                >
                    {label}
                </span>
            </header>

            <div
                className={`whitespace-pre-line text-sm leading-relaxed ${expanded ? "" : "line-clamp-3"}`}
                style={{ color: "rgba(255,255,255,0.85)" }}
            >
                {item.content}
            </div>

            <button
                type="button"
                onClick={() => setExpanded((v) => !v)}
                aria-expanded={expanded}
                className="mt-3 flex items-center gap-1 text-xs font-medium transition-colors duration-200"
                style={{ color: C_ACCENT }}
            >
                {expanded ? "Réduire" : "Lire le coaching complet"}
                <ChevronDown
                    size={14}
                    strokeWidth={1.75}
                    style={{ transform: expanded ? "rotate(180deg)" : "rotate(0)", transition: "transform 200ms" }}
                />
            </button>
        </article>
    );
}

export const CoachingDetailCard = memo(CoachingDetailCardImpl);
