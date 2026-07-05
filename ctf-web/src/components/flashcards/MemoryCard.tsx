"use client";

import { motion, useReducedMotion } from "framer-motion";
import { Hexagon, Check } from "lucide-react";
import type { TileTone } from "@/lib/flashcards-memory-demo";

type Props = {
    label: string;
    badge: string;
    tone: TileTone;
    revealed: boolean; // face visible (sélectionnée en cours d'évaluation)
    matched: boolean; // paire trouvée → verrouillée
    wrong: boolean; // feedback FAUX transitoire
    disabled: boolean;
    onClick: () => void;
};

// Couleurs de badge par tonalité (tokens uniquement).
const TONE: Record<TileTone, { bg: string; fg: string }> = {
    risk: { bg: "var(--danger-subtle)", fg: "var(--danger-t)" },
    counter: { bg: "var(--accent-subtle)", fg: "var(--accent)" },
    term: { bg: "color-mix(in srgb, var(--accent-2) 16%, transparent)", fg: "var(--accent-2)" },
    def: { bg: "var(--surface-2)", fg: "var(--text-2)" },
};

/** Tuile Memory : flip 3D (dos à motif cyber / face contenu) + feedback juste (matched) / faux (wrong). */
export default function MemoryCard({ label, badge, tone, revealed, matched, wrong, disabled, onClick }: Props) {
    const reduce = useReducedMotion();
    const faceUp = revealed || matched;
    const toneColor = TONE[tone];

    const faceBase: React.CSSProperties = {
        position: "absolute",
        inset: 0,
        display: "flex",
        backfaceVisibility: "hidden",
        WebkitBackfaceVisibility: "hidden",
        borderRadius: 14,
        border: "1px solid var(--border)",
        padding: 8,
    };

    return (
        <div style={{ perspective: 900 }}>
            <motion.button
                type="button"
                onClick={onClick}
                disabled={disabled}
                aria-label={faceUp ? `${badge} : ${label}` : "Carte cachée, cliquez pour révéler"}
                aria-pressed={faceUp}
                animate={
                    wrong && !reduce
                        ? { rotateY: faceUp ? 180 : 0, x: [0, -6, 6, -4, 4, 0] }
                        : { rotateY: faceUp ? 180 : 0, x: 0 }
                }
                transition={reduce ? { duration: 0 } : { duration: 0.4, ease: [0.22, 1, 0.36, 1] }}
                whileHover={disabled || faceUp || reduce ? undefined : { y: -2 }}
                style={{
                    position: "relative",
                    transformStyle: "preserve-3d",
                    width: "100%",
                    aspectRatio: "1 / 1",
                    minHeight: 92,
                    cursor: disabled ? "default" : "pointer",
                    background: "transparent",
                    border: "none",
                    padding: 0,
                }}
            >
                {/* Dos : motif cyber (trame de points accent + monogramme hexagonal Sentys) — identique partout */}
                <div
                    style={{
                        ...faceBase,
                        alignItems: "center",
                        justifyContent: "center",
                        background: "var(--surface-2)",
                        backgroundImage:
                            "radial-gradient(circle, color-mix(in srgb, var(--accent) 22%, transparent) 1.1px, transparent 1.1px)",
                        backgroundSize: "11px 11px",
                        borderColor: "var(--accent-border)",
                        overflow: "hidden",
                    }}
                    aria-hidden={faceUp}
                >
                    <span className="relative flex items-center justify-center">
                        <Hexagon className="h-9 w-9" strokeWidth={1.25} style={{ color: "var(--accent)" }} />
                        <span
                            className="absolute text-[13px] font-black leading-none"
                            style={{ color: "var(--accent)" }}
                        >
                            S
                        </span>
                    </span>
                </div>

                {/* Face visible (contenu) */}
                <div
                    style={{
                        ...faceBase,
                        transform: "rotateY(180deg)",
                        flexDirection: "column",
                        alignItems: "flex-start",
                        justifyContent: "space-between",
                        overflow: "hidden",
                        background: matched ? "var(--accent-subtle)" : "var(--surface)",
                        borderColor: matched ? "var(--accent)" : wrong ? "var(--danger)" : "var(--border)",
                    }}
                    aria-hidden={!faceUp}
                >
                    <span
                        className="rounded-full px-1.5 py-0.5 text-[9px] font-semibold uppercase tracking-wide"
                        style={{ background: toneColor.bg, color: toneColor.fg }}
                    >
                        {badge}
                    </span>
                    <span
                        className="line-clamp-3 text-[11px] font-semibold leading-tight"
                        style={{ color: "var(--text)" }}
                    >
                        {label}
                    </span>
                    {matched ? (
                        <motion.span
                            initial={reduce ? { opacity: 0 } : { scale: 0, opacity: 0 }}
                            animate={reduce ? { opacity: 1 } : { scale: 1, opacity: 1 }}
                            transition={reduce ? { duration: 0 } : { type: "spring", stiffness: 500, damping: 18 }}
                            className="self-end"
                        >
                            <Check className="h-4 w-4" style={{ color: "var(--accent)" }} />
                        </motion.span>
                    ) : (
                        <span aria-hidden className="h-4" />
                    )}
                </div>
            </motion.button>
        </div>
    );
}
