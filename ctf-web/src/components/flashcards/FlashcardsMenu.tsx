"use client";

import { motion, useReducedMotion } from "framer-motion";
import { BookOpen, Timer, Layers, ArrowRight } from "lucide-react";

type Props = {
    total: number;
    onPick: (mode: "revision" | "epreuve") => void;
};

/** Écran d'accueil du menu de test flashcards : choix Révision / Épreuve. */
export default function FlashcardsMenu({ total, onPick }: Props) {
    const reduce = useReducedMotion();
    const cards = [
        {
            mode: "revision" as const,
            icon: BookOpen,
            title: "Révision",
            desc: "Apprentissage libre. Retournez chaque carte pour découvrir la réponse, à votre rythme. Sans score.",
            cta: "Réviser",
        },
        {
            mode: "epreuve" as const,
            icon: Timer,
            title: "Épreuve",
            desc: "Répondez au QCM contre la montre. Feedback immédiat juste / faux, chrono et score en fin de session.",
            cta: "Se tester",
        },
    ];

    return (
        <div className="mx-auto flex w-full max-w-2xl flex-col gap-8">
            <div className="text-center">
                <span className="inline-flex items-center gap-2 rounded-full bg-accent/10 px-3 py-1 text-xs font-medium text-accent">
                    <Layers className="h-3.5 w-3.5" /> Menu de test isolé
                </span>
                <h1 className="mt-4 text-3xl font-bold text-[var(--text)]">Flashcards cyber</h1>
                <p className="mt-2 text-sm text-[var(--text-2)]">
                    {total} cartes de démonstration. Choisissez un mode pour commencer.
                </p>
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
                {cards.map((c, i) => (
                    <motion.button
                        key={c.mode}
                        onClick={() => onPick(c.mode)}
                        initial={reduce ? false : { opacity: 0, y: 8 }}
                        animate={{ opacity: 1, y: 0 }}
                        transition={{ duration: 0.25, delay: reduce ? 0 : i * 0.06 }}
                        whileHover={reduce ? undefined : { y: -2 }}
                        className="group flex flex-col gap-3 rounded-2xl border border-border bg-surface p-6 text-left transition-colors duration-200 hover:border-[var(--accent-border)] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--accent)]"
                    >
                        <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-accent/10 text-accent">
                            <c.icon className="h-5 w-5" />
                        </span>
                        <span className="text-lg font-bold text-[var(--text)]">{c.title}</span>
                        <span className="text-sm leading-relaxed text-[var(--text-2)]">{c.desc}</span>
                        <span className="mt-2 inline-flex items-center gap-1.5 text-sm font-medium text-accent">
                            {c.cta} <ArrowRight className="h-4 w-4 transition-transform duration-200 group-hover:translate-x-0.5" />
                        </span>
                    </motion.button>
                ))}
            </div>
        </div>
    );
}
