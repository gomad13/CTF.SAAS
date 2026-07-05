"use client";

import { motion, useReducedMotion } from "framer-motion";
import type { ReactNode } from "react";

type FlipCardProps = {
    flipped: boolean;
    onToggle: () => void;
    front: ReactNode;
    back: ReactNode;
    /** Libellé accessible du bouton (annonce l'action de retournement). */
    ariaLabel?: string;
};

const faceBase: React.CSSProperties = {
    position: "absolute",
    inset: 0,
    display: "flex",
    flexDirection: "column",
    backfaceVisibility: "hidden",
    WebkitBackfaceVisibility: "hidden",
    borderRadius: 18,
    border: "1px solid var(--border)",
    background: "var(--surface)",
    padding: 28,
    overflow: "auto",
};

/**
 * Carte à retournement 3D (rotateY). Deux faces empilées, backface masquée.
 * Cliquable + focusable clavier (Entrée/Espace) pour retourner. reduced-motion : bascule instantanée.
 */
export default function FlipCard({ flipped, onToggle, front, back, ariaLabel }: FlipCardProps) {
    const reduce = useReducedMotion();

    return (
        <div style={{ perspective: 1200, width: "100%" }}>
            <motion.button
                type="button"
                onClick={onToggle}
                aria-label={ariaLabel ?? "Retourner la carte"}
                aria-pressed={flipped}
                animate={{ rotateY: flipped ? 180 : 0 }}
                transition={reduce ? { duration: 0 } : { duration: 0.5, ease: [0.22, 1, 0.36, 1] }}
                style={{
                    position: "relative",
                    transformStyle: "preserve-3d",
                    width: "100%",
                    minHeight: 320,
                    cursor: "pointer",
                    textAlign: "left",
                    background: "transparent",
                    border: "none",
                    padding: 0,
                }}
            >
                {/* Recto (face avant) */}
                <div style={faceBase} aria-hidden={flipped}>
                    {front}
                </div>
                {/* Verso */}
                <div style={{ ...faceBase, transform: "rotateY(180deg)" }} aria-hidden={!flipped}>
                    {back}
                </div>
            </motion.button>
        </div>
    );
}
