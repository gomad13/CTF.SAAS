"use client";

import { useCallback, useState } from "react";
import { AnimatePresence, motion, useReducedMotion } from "framer-motion";
import { ArrowLeft, CheckCircle2, XCircle, Clock, ArrowRight } from "lucide-react";
import type { Flashcard } from "@/lib/flashcards-demo";
import { useSessionTimer } from "./useSessionTimer";
import SessionRecap from "./SessionRecap";

type Feedback = "idle" | "juste" | "faux";
type Props = { cards: Flashcard[]; onExit: () => void };

/** Mode Épreuve (type memory) : QCM, feedback animé JUSTE/FAUX, chrono de session, score + récap. */
export default function EpreuveMode({ cards, onExit }: Props) {
    const reduce = useReducedMotion();
    const [index, setIndex] = useState(0);
    const [selected, setSelected] = useState<number | null>(null);
    const [feedback, setFeedback] = useState<Feedback>("idle");
    const [correct, setCorrect] = useState(0);
    const [finished, setFinished] = useState(false);

    const timer = useSessionTimer(!finished);
    const card = cards[index];
    const isLast = index === cards.length - 1;

    const answer = useCallback(
        (choiceIndex: number) => {
            if (feedback !== "idle") return; // une seule réponse par carte
            const ok = choiceIndex === card.correctIndex;
            setSelected(choiceIndex);
            setFeedback(ok ? "juste" : "faux");
            if (ok) setCorrect((c) => c + 1);
        },
        [feedback, card.correctIndex],
    );

    const next = useCallback(() => {
        if (isLast) {
            setFinished(true);
            return;
        }
        setIndex((i) => i + 1);
        setSelected(null);
        setFeedback("idle");
    }, [isLast]);

    const replay = useCallback(() => {
        setIndex(0);
        setSelected(null);
        setFeedback("idle");
        setCorrect(0);
        setFinished(false);
        timer.reset();
    }, [timer]);

    if (finished) {
        return (
            <SessionRecap
                correct={correct}
                total={cards.length}
                timeLabel={timer.label}
                onReplay={replay}
                onExit={onExit}
            />
        );
    }

    // Variants de feedback appliqués au conteneur de carte
    const cardAnim =
        feedback === "juste"
            ? { scale: reduce ? 1 : [1, 1.06, 1] }
            : feedback === "faux"
              ? { x: reduce ? 0 : [0, -8, 8, -6, 6, 0] }
              : { scale: 1, x: 0 };

    const choiceClass = (i: number): string => {
        const base =
            "flex w-full items-center gap-3 rounded-xl border px-4 py-3 text-left text-sm font-medium transition-colors duration-200 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--accent)]";
        if (feedback === "idle")
            return `${base} border-border bg-surface-2 text-[var(--text)] hover:border-[var(--accent-border)] hover:bg-surface cursor-pointer`;
        if (i === card.correctIndex) return `${base} border-success bg-success/10 text-[var(--text)]`;
        if (i === selected) return `${base} border-danger bg-danger/10 text-[var(--text)]`;
        return `${base} border-border bg-surface-2 text-[var(--text-3)] opacity-60`;
    };

    return (
        <div className="mx-auto flex w-full max-w-2xl flex-col gap-5">
            {/* Barre haut : menu, progression, chrono */}
            <div className="flex items-center justify-between">
                <button
                    onClick={onExit}
                    className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-medium text-[var(--text-2)] transition-colors duration-200 hover:bg-surface-2 focus-visible:outline focus-visible:outline-2 focus-visible:outline-[var(--accent)]"
                >
                    <ArrowLeft className="h-4 w-4" /> Menu
                </button>
                <div className="flex items-center gap-4">
                    <span className="text-sm font-medium text-[var(--text-3)]">
                        {index + 1} / {cards.length}
                    </span>
                    <span
                        className="inline-flex items-center gap-1.5 rounded-lg bg-surface-2 px-2.5 py-1 text-sm font-semibold tabular-nums text-[var(--text)]"
                        aria-live="off"
                        aria-label={`Temps écoulé ${timer.label}`}
                    >
                        <Clock className="h-4 w-4 text-[var(--text-3)]" /> {timer.label}
                    </span>
                </div>
            </div>

            {/* Barre de progression */}
            <div className="h-1.5 w-full overflow-hidden rounded-full bg-surface-2">
                <div
                    className="h-full rounded-full bg-accent transition-all duration-300"
                    style={{ width: `${((index + (feedback !== "idle" ? 1 : 0)) / cards.length) * 100}%` }}
                />
            </div>

            {/* Carte question */}
            <motion.div
                animate={cardAnim}
                transition={{ duration: reduce ? 0 : 0.45, ease: "easeInOut" }}
                className="relative rounded-2xl border border-border bg-surface p-6"
            >
                {/* Overlay glow feedback (opacité animée, couleur en token) */}
                <AnimatePresence>
                    {feedback !== "idle" && (
                        <motion.div
                            key={feedback}
                            initial={{ opacity: 0 }}
                            animate={{ opacity: reduce ? 0.5 : [0, 1, 0.35] }}
                            exit={{ opacity: 0 }}
                            transition={{ duration: reduce ? 0 : 0.6, ease: "easeOut" }}
                            aria-hidden
                            className="pointer-events-none absolute inset-0 rounded-2xl"
                            style={{
                                boxShadow: `inset 0 0 0 2px ${feedback === "juste" ? "var(--accent)" : "var(--danger)"}`,
                                background:
                                    feedback === "juste" ? "var(--accent-subtle)" : "var(--danger-subtle)",
                            }}
                        />
                    )}
                </AnimatePresence>

                <div className="relative">
                    <div className="flex items-center justify-between gap-3">
                        <span className="inline-flex rounded-full bg-accent/10 px-2.5 py-0.5 text-xs font-medium text-accent">
                            {card.category}
                        </span>
                        {/* Icône de feedback */}
                        <AnimatePresence>
                            {feedback !== "idle" && (
                                <motion.span
                                    initial={reduce ? { opacity: 0 } : { scale: 0, opacity: 0 }}
                                    animate={reduce ? { opacity: 1 } : { scale: 1, opacity: 1 }}
                                    exit={{ opacity: 0 }}
                                    transition={reduce ? { duration: 0 } : { type: "spring", stiffness: 500, damping: 18 }}
                                >
                                    {feedback === "juste" ? (
                                        <CheckCircle2 className="h-7 w-7 text-accent" />
                                    ) : (
                                        <XCircle className="h-7 w-7 text-danger" />
                                    )}
                                </motion.span>
                            )}
                        </AnimatePresence>
                    </div>

                    <p className="mt-4 text-lg font-bold leading-snug text-[var(--text)]">{card.front}</p>

                    <div className="mt-5 flex flex-col gap-2.5">
                        {card.choices.map((choice, i) => (
                            <button
                                key={i}
                                type="button"
                                disabled={feedback !== "idle"}
                                onClick={() => answer(i)}
                                className={choiceClass(i)}
                            >
                                <span
                                    aria-hidden
                                    className="flex h-6 w-6 shrink-0 items-center justify-center rounded-md border border-border text-xs font-semibold text-[var(--text-2)]"
                                >
                                    {String.fromCharCode(65 + i)}
                                </span>
                                <span className="flex-1">{choice}</span>
                            </button>
                        ))}
                    </div>

                    {/* Explication + bouton continuer après réponse */}
                    <AnimatePresence>
                        {feedback !== "idle" && (
                            <motion.div
                                initial={reduce ? false : { opacity: 0, height: 0 }}
                                animate={{ opacity: 1, height: "auto" }}
                                transition={{ duration: reduce ? 0 : 0.25 }}
                                className="overflow-hidden"
                            >
                                <p className="mt-4 rounded-xl bg-surface-2 p-3 text-sm leading-relaxed text-[var(--text-2)]">
                                    {feedback === "juste" ? "Bonne réponse. " : "Réponse attendue : "}
                                    {card.back}
                                </p>
                                <div className="mt-4 flex justify-end">
                                    <button
                                        onClick={next}
                                        autoFocus
                                        className="inline-flex items-center gap-2 rounded-lg bg-accent px-4 py-2 font-medium text-[var(--on-accent)] transition-colors duration-200 hover:bg-[var(--accent-hover)] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--accent)]"
                                    >
                                        {isLast ? "Voir le résultat" : "Suivante"} <ArrowRight className="h-4 w-4" />
                                    </button>
                                </div>
                            </motion.div>
                        )}
                    </AnimatePresence>
                </div>
            </motion.div>
        </div>
    );
}
