"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { motion, useReducedMotion } from "framer-motion";
import { ArrowLeft, Clock, MousePointerClick, Trophy, RotateCcw, ArrowRight } from "lucide-react";
import {
    buildShuffledDeck,
    MEMORY_PAIR_COUNT,
    type MemoryTile,
    type PairKind,
} from "@/lib/flashcards-memory-demo";
import { useSessionTimer } from "./useSessionTimer";
import MemoryCard from "./MemoryCard";
import MemoryVariantChoice from "./MemoryVariantChoice";

type Props = {
    onExit: () => void;
    /** Appelé une fois toutes les paires trouvées (scoring/complétion parcours). Optionnel (test isolé sans). */
    onFinish?: () => void;
    /** Libellé du bouton de sortie du récap. En parcours : « Continuer le parcours ». */
    continueLabel?: string;
};

/** Mode Memory : UNE variante d'association par partie (terme↔déf OU risque↔parade), choisie sur un écran dédié.
 *  Flip 3D au clic, détection de paire (juste/faux), chrono, compteur de coups, récap. */
export default function MemoryMode({ onExit, onFinish, continueLabel }: Props) {
    const reduce = useReducedMotion();
    const finishedRef = useRef(false);
    const [variant, setVariant] = useState<PairKind | null>(null);
    const [deck, setDeck] = useState<MemoryTile[]>([]);
    const [flipped, setFlipped] = useState<string[]>([]); // uids face visible (en évaluation)
    const [matched, setMatched] = useState<string[]>([]); // pairIds trouvés
    const [wrong, setWrong] = useState(false); // feedback FAUX transitoire
    const [moves, setMoves] = useState(0);
    const [lock, setLock] = useState(false);
    const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

    const finished = deck.length > 0 && matched.length === MEMORY_PAIR_COUNT;
    const { label: timerLabel, reset: resetTimer } = useSessionTimer(deck.length > 0 && !finished);

    // Remonte la fin de partie UNE fois (scoring/complétion parcours).
    useEffect(() => {
        if (finished && !finishedRef.current) { finishedRef.current = true; onFinish?.(); }
    }, [finished, onFinish]);

    const clearPending = useCallback(() => {
        if (timeoutRef.current) clearTimeout(timeoutRef.current);
        timeoutRef.current = null;
    }, []);

    // Démarre / redémarre une partie pour un type donné (deck filtré sur ce type).
    const start = useCallback(
        (kind: PairKind) => {
            clearPending();
            setDeck(buildShuffledDeck(kind));
            setFlipped([]);
            setMatched([]);
            setWrong(false);
            setMoves(0);
            setLock(false);
            resetTimer();
        },
        [clearPending, resetTimer],
    );

    // Construit la grille quand une variante est choisie ; nettoie si on revient à l'écran de choix.
    useEffect(() => {
        if (!variant) {
            setDeck([]);
            return;
        }
        start(variant);
        return () => clearPending();
    }, [variant, start, clearPending]);

    const backToChoice = useCallback(() => {
        clearPending();
        setVariant(null);
    }, [clearPending]);

    const onTile = useCallback(
        (tile: MemoryTile) => {
            if (lock || finished) return;
            if (matched.includes(tile.pairId) || flipped.includes(tile.uid)) return;

            if (flipped.length === 0) {
                setFlipped([tile.uid]);
                return;
            }
            // 2ᵉ carte → un coup + évaluation
            const firstUid = flipped[0];
            const first = deck.find((t) => t.uid === firstUid);
            setFlipped([firstUid, tile.uid]);
            setMoves((m) => m + 1);
            setLock(true);

            if (first && first.pairId === tile.pairId) {
                // JUSTE : on verrouille la paire après un court instant (le flip se termine)
                timeoutRef.current = setTimeout(() => {
                    setMatched((prev) => [...prev, tile.pairId]);
                    setFlipped([]);
                    setLock(false);
                }, 380);
            } else {
                // FAUX : shake + flash, puis re-retournement
                setWrong(true);
                timeoutRef.current = setTimeout(() => {
                    setFlipped([]);
                    setWrong(false);
                    setLock(false);
                }, 900);
            }
        },
        [lock, finished, matched, flipped, deck],
    );

    // Écran de choix du type — rendu APRÈS tous les hooks (règles des Hooks respectées).
    if (!variant) {
        return <MemoryVariantChoice onPick={setVariant} onExit={onExit} />;
    }

    const isTerme = variant === "terme-def";

    return (
        <div className="mx-auto flex w-full max-w-4xl flex-col gap-5">
            {/* Barre haut : retour au choix, coups, chrono */}
            <div className="flex items-center justify-between">
                <button
                    onClick={backToChoice}
                    className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-medium text-[var(--text-2)] transition-colors duration-200 hover:bg-surface-2 focus-visible:outline focus-visible:outline-2 focus-visible:outline-[var(--accent)]"
                >
                    <ArrowLeft className="h-4 w-4" /> Type
                </button>
                <div className="flex items-center gap-3">
                    <span className="inline-flex items-center gap-1.5 rounded-lg bg-surface-2 px-2.5 py-1 text-sm font-semibold tabular-nums text-[var(--text)]">
                        <MousePointerClick className="h-4 w-4 text-[var(--text-3)]" /> {moves} coups
                    </span>
                    <span
                        className="inline-flex items-center gap-1.5 rounded-lg bg-surface-2 px-2.5 py-1 text-sm font-semibold tabular-nums text-[var(--text)]"
                        aria-label={`Temps écoulé ${timerLabel}`}
                    >
                        <Clock className="h-4 w-4 text-[var(--text-3)]" /> {timerLabel}
                    </span>
                </div>
            </div>

            <div className="text-center">
                <p className="text-sm text-[var(--text-2)]">
                    {isTerme ? (
                        <>
                            Associez chaque <span className="font-semibold text-accent-2">terme</span> à sa{" "}
                            <span className="font-semibold text-[var(--text)]">définition</span>.
                        </>
                    ) : (
                        <>
                            Associez chaque <span className="font-semibold text-danger">risque</span> à sa{" "}
                            <span className="font-semibold text-accent">parade</span>.
                        </>
                    )}{" "}
                    Trouvées :{" "}
                    <span className="font-semibold text-[var(--text)]">
                        {matched.length}/{MEMORY_PAIR_COUNT}
                    </span>
                </p>
            </div>

            {/* Grille 6×2 sur desktop (3×4 sur mobile) */}
            <div className="grid grid-cols-3 gap-2.5 sm:grid-cols-6 sm:gap-3">
                {deck.map((tile) => (
                    <MemoryCard
                        key={tile.uid}
                        label={tile.label}
                        badge={tile.badge}
                        tone={tile.tone}
                        revealed={flipped.includes(tile.uid)}
                        matched={matched.includes(tile.pairId)}
                        wrong={wrong && flipped.includes(tile.uid)}
                        disabled={lock || matched.includes(tile.pairId) || finished}
                        onClick={() => onTile(tile)}
                    />
                ))}
            </div>

            {/* Récap de fin */}
            {finished && (
                <motion.div
                    initial={reduce ? false : { opacity: 0, y: 8 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.25 }}
                    className="mx-auto mt-2 flex w-full max-w-md flex-col items-center gap-5 rounded-2xl border border-border bg-surface p-8 text-center"
                >
                    <div
                        className="flex h-16 w-16 items-center justify-center rounded-full"
                        style={{ background: "var(--accent-subtle)" }}
                    >
                        <Trophy className="h-8 w-8" style={{ color: "var(--accent)" }} />
                    </div>
                    <div>
                        <h2 className="text-2xl font-bold text-[var(--text)]">Toutes les paires trouvées !</h2>
                        <p className="mt-1 text-sm text-[var(--text-2)]">Bien joué — mémoire cyber au point.</p>
                    </div>
                    <div className="grid w-full grid-cols-2 gap-3">
                        <div className="flex flex-col items-center gap-1 rounded-xl border border-border bg-surface-2 px-2 py-3">
                            <Clock className="h-4 w-4 text-[var(--text-2)]" />
                            <span className="text-lg font-bold tabular-nums text-[var(--text)]">{timerLabel}</span>
                            <span className="text-xs text-[var(--text-3)]">Temps</span>
                        </div>
                        <div className="flex flex-col items-center gap-1 rounded-xl border border-border bg-surface-2 px-2 py-3">
                            <MousePointerClick className="h-4 w-4 text-[var(--text-2)]" />
                            <span className="text-lg font-bold tabular-nums text-[var(--text)]">{moves}</span>
                            <span className="text-xs text-[var(--text-3)]">Coups</span>
                        </div>
                    </div>
                    {continueLabel && (
                        <button
                            onClick={onExit}
                            className="inline-flex w-full items-center justify-center gap-2 rounded-lg bg-accent px-4 py-3 font-semibold text-[var(--on-accent)] transition-colors duration-200 hover:bg-[var(--accent-hover)] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--accent)]"
                        >
                            {continueLabel} <ArrowRight className="h-4 w-4" />
                        </button>
                    )}
                    <div className="flex w-full gap-3">
                        <button
                            onClick={backToChoice}
                            className="inline-flex flex-1 items-center justify-center gap-2 rounded-lg border border-border px-4 py-2.5 font-medium text-[var(--text)] transition-colors duration-200 hover:bg-surface-2 focus-visible:outline focus-visible:outline-2 focus-visible:outline-[var(--accent)]"
                        >
                            <ArrowLeft className="h-4 w-4" /> Type
                        </button>
                        <button
                            onClick={() => start(variant)}
                            className="inline-flex flex-1 items-center justify-center gap-2 rounded-lg border border-border px-4 py-2.5 font-medium text-[var(--text)] transition-colors duration-200 hover:bg-surface-2 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--accent)]"
                        >
                            <RotateCcw className="h-4 w-4" /> Rejouer
                        </button>
                    </div>
                </motion.div>
            )}
        </div>
    );
}
