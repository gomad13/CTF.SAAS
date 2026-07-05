"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { motion, useReducedMotion } from "framer-motion";
import { ArrowLeft, Clock, MousePointerClick, Trophy, RotateCcw } from "lucide-react";
import { buildShuffledDeck, MEMORY_PAIR_COUNT, type MemoryTile } from "@/lib/flashcards-memory-demo";
import { useSessionTimer } from "./useSessionTimer";
import MemoryCard from "./MemoryCard";

type Props = { onExit: () => void };

/** Mode Memory : grille de paires risque ↔ contre-mesure, flip au clic, détection de paire (juste/faux),
 *  chrono de session, compteur de coups, récap de fin. Isolé — ne touche pas aux autres modes. */
export default function MemoryMode({ onExit }: Props) {
    const reduce = useReducedMotion();
    const [deck, setDeck] = useState<MemoryTile[]>([]);
    const [flipped, setFlipped] = useState<string[]>([]); // uids face visible (en évaluation)
    const [matched, setMatched] = useState<string[]>([]); // pairIds trouvés
    const [wrong, setWrong] = useState(false); // feedback FAUX transitoire
    const [moves, setMoves] = useState(0);
    const [lock, setLock] = useState(false);
    const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

    const finished = deck.length > 0 && matched.length === MEMORY_PAIR_COUNT;
    const timer = useSessionTimer(deck.length > 0 && !finished);

    const clearPending = () => {
        if (timeoutRef.current) clearTimeout(timeoutRef.current);
        timeoutRef.current = null;
    };

    const newGame = useCallback(() => {
        clearPending();
        setDeck(buildShuffledDeck());
        setFlipped([]);
        setMatched([]);
        setWrong(false);
        setMoves(0);
        setLock(false);
        timer.reset();
    }, [timer]);

    // Mélange côté client uniquement (évite tout mismatch d'hydratation SSR).
    useEffect(() => {
        setDeck(buildShuffledDeck());
        return () => clearPending();
    }, []);

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

    return (
        <div className="mx-auto flex w-full max-w-4xl flex-col gap-5">
            {/* Barre haut : menu, coups, chrono */}
            <div className="flex items-center justify-between">
                <button
                    onClick={onExit}
                    className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-medium text-[var(--text-2)] transition-colors duration-200 hover:bg-surface-2 focus-visible:outline focus-visible:outline-2 focus-visible:outline-[var(--accent)]"
                >
                    <ArrowLeft className="h-4 w-4" /> Menu
                </button>
                <div className="flex items-center gap-3">
                    <span className="inline-flex items-center gap-1.5 rounded-lg bg-surface-2 px-2.5 py-1 text-sm font-semibold tabular-nums text-[var(--text)]">
                        <MousePointerClick className="h-4 w-4 text-[var(--text-3)]" /> {moves} coups
                    </span>
                    <span
                        className="inline-flex items-center gap-1.5 rounded-lg bg-surface-2 px-2.5 py-1 text-sm font-semibold tabular-nums text-[var(--text)]"
                        aria-label={`Temps écoulé ${timer.label}`}
                    >
                        <Clock className="h-4 w-4 text-[var(--text-3)]" /> {timer.label}
                    </span>
                </div>
            </div>

            <div className="text-center">
                <p className="text-sm text-[var(--text-2)]">
                    Retrouvez les paires{" "}
                    <span className="font-semibold text-danger">risque</span> ↔{" "}
                    <span className="font-semibold text-accent">parade</span> et{" "}
                    <span className="font-semibold text-accent-2">terme</span> ↔ définition. Trouvées :{" "}
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
                            <span className="text-lg font-bold tabular-nums text-[var(--text)]">{timer.label}</span>
                            <span className="text-xs text-[var(--text-3)]">Temps</span>
                        </div>
                        <div className="flex flex-col items-center gap-1 rounded-xl border border-border bg-surface-2 px-2 py-3">
                            <MousePointerClick className="h-4 w-4 text-[var(--text-2)]" />
                            <span className="text-lg font-bold tabular-nums text-[var(--text)]">{moves}</span>
                            <span className="text-xs text-[var(--text-3)]">Coups</span>
                        </div>
                    </div>
                    <div className="flex w-full gap-3">
                        <button
                            onClick={onExit}
                            className="inline-flex flex-1 items-center justify-center gap-2 rounded-lg border border-border px-4 py-2.5 font-medium text-[var(--text)] transition-colors duration-200 hover:bg-surface-2 focus-visible:outline focus-visible:outline-2 focus-visible:outline-[var(--accent)]"
                        >
                            <ArrowLeft className="h-4 w-4" /> Menu
                        </button>
                        <button
                            onClick={newGame}
                            className="inline-flex flex-1 items-center justify-center gap-2 rounded-lg bg-accent px-4 py-2.5 font-medium text-[var(--on-accent)] transition-colors duration-200 hover:bg-[var(--accent-hover)] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--accent)]"
                        >
                            <RotateCcw className="h-4 w-4" /> Rejouer
                        </button>
                    </div>
                </motion.div>
            )}
        </div>
    );
}
