"use client";

import { useState } from "react";
import { ArrowLeft, RotateCcw, ChevronLeft, ChevronRight } from "lucide-react";
import type { Flashcard } from "@/lib/flashcards-demo";
import FlipCard from "./FlipCard";

type Props = {
    cards: Flashcard[];
    onExit: () => void;
};

/** Mode Révision : apprentissage libre, flip recto/verso, navigation précédente/suivante. Pas de score. */
export default function RevisionMode({ cards, onExit }: Props) {
    const [index, setIndex] = useState(0);
    const [flipped, setFlipped] = useState(false);
    const card = cards[index];

    const go = (delta: number) => {
        setFlipped(false);
        setIndex((i) => (i + delta + cards.length) % cards.length);
    };

    return (
        <div className="mx-auto flex w-full max-w-2xl flex-col gap-6">
            <div className="flex items-center justify-between">
                <button
                    onClick={onExit}
                    className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-medium text-[var(--text-2)] transition-colors duration-200 hover:bg-surface-2 focus-visible:outline focus-visible:outline-2 focus-visible:outline-[var(--accent)]"
                >
                    <ArrowLeft className="h-4 w-4" /> Menu
                </button>
                <span className="text-sm font-medium text-[var(--text-3)]">
                    Carte {index + 1} / {cards.length}
                </span>
            </div>

            <FlipCard
                flipped={flipped}
                onToggle={() => setFlipped((f) => !f)}
                ariaLabel={flipped ? "Revoir la question" : "Afficher la réponse"}
                front={
                    <>
                        <span className="inline-flex w-fit rounded-full bg-accent/10 px-2.5 py-0.5 text-xs font-medium text-accent">
                            {card.category}
                        </span>
                        <p className="mt-4 text-xl font-bold leading-snug text-[var(--text)]">{card.front}</p>
                        <span className="mt-auto pt-6 text-sm text-[var(--text-3)]">Cliquez pour retourner la carte →</span>
                    </>
                }
                back={
                    <>
                        <span className="inline-flex w-fit rounded-full bg-surface-2 px-2.5 py-0.5 text-xs font-medium text-[var(--text-2)]">
                            Réponse
                        </span>
                        <p className="mt-4 text-base leading-relaxed text-[var(--text)]">{card.back}</p>
                        <span className="mt-auto pt-6 text-sm text-[var(--text-3)]">← Cliquez pour revenir</span>
                    </>
                }
            />

            <div className="flex items-center justify-between gap-3">
                <button
                    onClick={() => go(-1)}
                    className="inline-flex items-center gap-2 rounded-lg border border-border px-4 py-2 font-medium text-[var(--text)] transition-colors duration-200 hover:bg-surface-2 focus-visible:outline focus-visible:outline-2 focus-visible:outline-[var(--accent)]"
                >
                    <ChevronLeft className="h-4 w-4" /> Précédente
                </button>

                <button
                    onClick={() => setFlipped((f) => !f)}
                    className="inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium text-[var(--text-2)] transition-colors duration-200 hover:bg-surface-2 focus-visible:outline focus-visible:outline-2 focus-visible:outline-[var(--accent)]"
                >
                    <RotateCcw className="h-4 w-4" /> Retourner
                </button>

                <button
                    onClick={() => go(1)}
                    className="inline-flex items-center gap-2 rounded-lg bg-accent px-4 py-2 font-medium text-[var(--on-accent)] transition-colors duration-200 hover:bg-[var(--accent-hover)] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--accent)]"
                >
                    Suivante <ChevronRight className="h-4 w-4" />
                </button>
            </div>
        </div>
    );
}
