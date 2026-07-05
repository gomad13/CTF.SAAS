"use client";

import { motion, useReducedMotion } from "framer-motion";
import { ShieldQuestion, Check } from "lucide-react";

type Props = {
    label: string;
    role: "risk" | "counter";
    revealed: boolean; // face visible (sélectionnée en cours d'évaluation)
    matched: boolean; // paire trouvée → verrouillée
    wrong: boolean; // feedback FAUX transitoire
    disabled: boolean;
    onClick: () => void;
};

/** Tuile du jeu Memory : flip 3D face-cachée/face-visible + feedback juste (matched) / faux (wrong). */
export default function MemoryCard({ label, role, revealed, matched, wrong, disabled, onClick }: Props) {
    const reduce = useReducedMotion();
    const faceUp = revealed || matched;

    const faceBase: React.CSSProperties = {
        position: "absolute",
        inset: 0,
        display: "flex",
        backfaceVisibility: "hidden",
        WebkitBackfaceVisibility: "hidden",
        borderRadius: 14,
        border: "1px solid var(--border)",
        padding: 10,
    };

    return (
        <div style={{ perspective: 900 }}>
            <motion.button
                type="button"
                onClick={onClick}
                disabled={disabled}
                aria-label={faceUp ? label : "Carte cachée, cliquez pour révéler"}
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
                    aspectRatio: "3 / 4",
                    minHeight: 96,
                    cursor: disabled ? "default" : "pointer",
                    background: "transparent",
                    border: "none",
                    padding: 0,
                }}
            >
                {/* Face cachée (dos) */}
                <div
                    style={{
                        ...faceBase,
                        alignItems: "center",
                        justifyContent: "center",
                        background: "var(--surface-2)",
                    }}
                    aria-hidden={faceUp}
                >
                    <ShieldQuestion className="h-6 w-6" style={{ color: "var(--text-3)" }} />
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
                        borderColor: matched
                            ? "var(--accent)"
                            : wrong
                              ? "var(--danger)"
                              : "var(--border)",
                    }}
                    aria-hidden={!faceUp}
                >
                    <span
                        className="rounded-full px-1.5 py-0.5 text-[10px] font-medium uppercase tracking-wide"
                        style={{
                            background: role === "risk" ? "var(--danger-subtle)" : "var(--accent-subtle)",
                            color: role === "risk" ? "var(--danger-t)" : "var(--accent)",
                        }}
                    >
                        {role === "risk" ? "Risque" : "Parade"}
                    </span>
                    <span className="text-xs font-semibold leading-tight" style={{ color: "var(--text)" }}>
                        {label}
                    </span>
                    {matched && (
                        <motion.span
                            initial={reduce ? { opacity: 0 } : { scale: 0, opacity: 0 }}
                            animate={reduce ? { opacity: 1 } : { scale: 1, opacity: 1 }}
                            transition={reduce ? { duration: 0 } : { type: "spring", stiffness: 500, damping: 18 }}
                            className="self-end"
                        >
                            <Check className="h-4 w-4" style={{ color: "var(--accent)" }} />
                        </motion.span>
                    )}
                </div>
            </motion.button>
        </div>
    );
}
