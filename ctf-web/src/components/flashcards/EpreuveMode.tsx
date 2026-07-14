"use client";

import { useCallback, useState } from "react";
import { AnimatePresence, motion, useReducedMotion } from "framer-motion";
import { ArrowLeft, CheckCircle2, XCircle, Clock, ArrowRight } from "lucide-react";
import type { Flashcard } from "@/lib/flashcards-demo";
import { useSessionTimer } from "./useSessionTimer";
import SessionRecap from "./SessionRecap";

type Feedback = "idle" | "juste" | "faux";
type Props = {
    cards: Flashcard[];
    onExit: () => void;
    /** Appelé une fois l'épreuve terminée (avant le récap) — sert au scoring parcours. */
    onFinish?: (correct: number, total: number) => void;
};

/** Mode Épreuve : QCM, feedback animé JUSTE/FAUX, chrono, score + récap. Charte violet Vision (tokens --v-*). */
export default function EpreuveMode({ cards, onExit, onFinish }: Props) {
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
            if (feedback !== "idle") return;
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
            onFinish?.(correct, cards.length);
            return;
        }
        setIndex((i) => i + 1);
        setSelected(null);
        setFeedback("idle");
    }, [isLast, onFinish, correct, cards.length]);

    const replay = useCallback(() => {
        setIndex(0); setSelected(null); setFeedback("idle"); setCorrect(0); setFinished(false); timer.reset();
    }, [timer]);

    if (finished) {
        return <SessionRecap correct={correct} total={cards.length} timeLabel={timer.label} onReplay={replay} onExit={onExit} />;
    }

    const cardAnim =
        feedback === "juste" ? { scale: reduce ? 1 : [1, 1.06, 1] }
            : feedback === "faux" ? { x: reduce ? 0 : [0, -8, 8, -6, 6, 0] }
                : { scale: 1, x: 0 };

    function choiceStyle(i: number): React.CSSProperties {
        const base: React.CSSProperties = { display: "flex", width: "100%", alignItems: "center", gap: 12, borderRadius: 12, padding: "12px 16px", textAlign: "left", fontSize: 14, fontWeight: 500, border: "1px solid var(--v-border)", transition: "border-color .2s ease, background-color .2s ease", background: "var(--v-surface-2)", color: "var(--v-text)" };
        if (feedback === "idle") return { ...base, cursor: "pointer" };
        if (i === card.correctIndex) return { ...base, borderColor: "var(--v-success)", background: "color-mix(in srgb, var(--v-success) 12%, transparent)" };
        if (i === selected) return { ...base, borderColor: "var(--v-danger)", background: "color-mix(in srgb, var(--v-danger) 12%, transparent)" };
        return { ...base, color: "var(--v-text-3)", opacity: 0.6 };
    }

    const btnFocus = "focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2";

    return (
        <div className="mx-auto flex w-full max-w-2xl flex-col gap-5">
            {/* Barre haut */}
            <div className="flex items-center justify-between">
                <button onClick={onExit} className={btnFocus}
                    style={{ display: "inline-flex", alignItems: "center", gap: 8, borderRadius: 10, border: "1px solid var(--v-border)", padding: "8px 12px", fontSize: 14, fontWeight: 500, color: "var(--v-text-2)", background: "transparent", cursor: "pointer" }}>
                    <ArrowLeft className="h-4 w-4" /> Menu
                </button>
                <div className="flex items-center gap-4">
                    <span style={{ fontSize: 14, fontWeight: 500, color: "var(--v-text-3)" }}>{index + 1} / {cards.length}</span>
                    <span aria-label={`Temps écoulé ${timer.label}`}
                        style={{ display: "inline-flex", alignItems: "center", gap: 6, borderRadius: 10, background: "var(--v-surface-2)", padding: "4px 10px", fontSize: 14, fontWeight: 600, fontVariantNumeric: "tabular-nums", color: "var(--v-text)" }}>
                        <Clock className="h-4 w-4" style={{ color: "var(--v-text-3)" }} /> {timer.label}
                    </span>
                </div>
            </div>

            {/* Progression */}
            <div style={{ height: 6, width: "100%", overflow: "hidden", borderRadius: 999, background: "var(--v-surface-2)" }}>
                <div style={{ height: "100%", borderRadius: 999, background: "var(--v-grad)", transition: "width .3s ease", width: `${((index + (feedback !== "idle" ? 1 : 0)) / cards.length) * 100}%` }} />
            </div>

            {/* Carte question */}
            <motion.div animate={cardAnim} transition={{ duration: reduce ? 0 : 0.45, ease: "easeInOut" }}
                style={{ position: "relative", borderRadius: 18, border: "1px solid var(--v-border)", background: "var(--v-surface)", padding: 24 }}>
                <AnimatePresence>
                    {feedback !== "idle" && (
                        <motion.div key={feedback} initial={{ opacity: 0 }} animate={{ opacity: reduce ? 0.5 : [0, 1, 0.35] }} exit={{ opacity: 0 }}
                            transition={{ duration: reduce ? 0 : 0.6, ease: "easeOut" }} aria-hidden
                            style={{ pointerEvents: "none", position: "absolute", inset: 0, borderRadius: 18, boxShadow: `inset 0 0 0 2px ${feedback === "juste" ? "var(--v-success)" : "var(--v-danger)"}`, background: feedback === "juste" ? "color-mix(in srgb, var(--v-success) 12%, transparent)" : "color-mix(in srgb, var(--v-danger) 12%, transparent)" }} />
                    )}
                </AnimatePresence>

                <div style={{ position: "relative" }}>
                    <div className="flex items-center justify-between gap-3">
                        <span style={{ display: "inline-flex", borderRadius: 999, background: "color-mix(in srgb, var(--v-accent) 15%, transparent)", padding: "2px 10px", fontSize: 12, fontWeight: 500, color: "var(--v-accent)" }}>{card.category}</span>
                        <AnimatePresence>
                            {feedback !== "idle" && (
                                <motion.span initial={reduce ? { opacity: 0 } : { scale: 0, opacity: 0 }} animate={reduce ? { opacity: 1 } : { scale: 1, opacity: 1 }} exit={{ opacity: 0 }}
                                    transition={reduce ? { duration: 0 } : { type: "spring", stiffness: 500, damping: 18 }}>
                                    {feedback === "juste" ? <CheckCircle2 className="h-7 w-7" style={{ color: "var(--v-success)" }} /> : <XCircle className="h-7 w-7" style={{ color: "var(--v-danger)" }} />}
                                </motion.span>
                            )}
                        </AnimatePresence>
                    </div>

                    <p style={{ marginTop: 16, fontSize: 18, fontWeight: 700, lineHeight: 1.35, color: "var(--v-text)" }}>{card.front}</p>

                    <div className="mt-5 flex flex-col gap-2.5">
                        {card.choices.map((choice, i) => (
                            <button key={i} type="button" disabled={feedback !== "idle"} onClick={() => answer(i)} className={btnFocus} style={choiceStyle(i)}>
                                <span aria-hidden style={{ display: "flex", height: 24, width: 24, flexShrink: 0, alignItems: "center", justifyContent: "center", borderRadius: 6, border: "1px solid var(--v-border)", fontSize: 12, fontWeight: 600, color: "var(--v-text-2)" }}>{String.fromCharCode(65 + i)}</span>
                                <span style={{ flex: 1 }}>{choice}</span>
                            </button>
                        ))}
                    </div>

                    <AnimatePresence>
                        {feedback !== "idle" && (
                            <motion.div initial={reduce ? false : { opacity: 0, height: 0 }} animate={{ opacity: 1, height: "auto" }} transition={{ duration: reduce ? 0 : 0.25 }} style={{ overflow: "hidden" }}>
                                <p style={{ marginTop: 16, borderRadius: 12, background: "var(--v-surface-2)", padding: 12, fontSize: 14, lineHeight: 1.6, color: "var(--v-text-2)" }}>
                                    {feedback === "juste" ? "Bonne réponse. " : "Réponse attendue : "}{card.back}
                                </p>
                                <div className="mt-4 flex justify-end">
                                    <button onClick={next} autoFocus className={btnFocus}
                                        style={{ display: "inline-flex", alignItems: "center", gap: 8, borderRadius: 10, background: "var(--v-grad)", padding: "8px 16px", fontWeight: 600, color: "var(--v-text)", border: "none", cursor: "pointer", boxShadow: "0 6px 16px color-mix(in srgb, var(--v-accent) 40%, transparent)" }}>
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
