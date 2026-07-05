"use client";

import { motion, useReducedMotion } from "framer-motion";
import { BookText, ShieldAlert, ArrowLeft, ArrowRight } from "lucide-react";
import type { PairKind } from "@/lib/flashcards-memory-demo";

type Props = {
    onPick: (kind: PairKind) => void;
    onExit: () => void;
};

/** Écran de choix du type de partie Memory : une seule famille d'association par partie (jouabilité). */
export default function MemoryVariantChoice({ onPick, onExit }: Props) {
    const reduce = useReducedMotion();
    const options = [
        {
            kind: "terme-def" as const,
            icon: BookText,
            title: "Terme / Définition",
            desc: "Associez chaque terme cyber à sa définition. 6 paires, un seul type par partie.",
        },
        {
            kind: "risque-parade" as const,
            icon: ShieldAlert,
            title: "Risque / Parade",
            desc: "Associez chaque risque à sa contre-mesure. 6 paires, un seul type par partie.",
        },
    ];

    return (
        <div className="mx-auto flex w-full max-w-2xl flex-col gap-8">
            <div className="flex items-center">
                <button
                    onClick={onExit}
                    className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-medium text-[var(--text-2)] transition-colors duration-200 hover:bg-surface-2 focus-visible:outline focus-visible:outline-2 focus-visible:outline-[var(--accent)]"
                >
                    <ArrowLeft className="h-4 w-4" /> Menu
                </button>
            </div>

            <div className="text-center">
                <h1 className="text-2xl font-bold text-[var(--text)]">Memory — choisissez un type</h1>
                <p className="mt-2 text-sm text-[var(--text-2)]">
                    Une partie ne mélange qu&apos;un seul type d&apos;association, pour rester jouable.
                </p>
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
                {options.map((o, i) => (
                    <motion.button
                        key={o.kind}
                        onClick={() => onPick(o.kind)}
                        initial={reduce ? false : { opacity: 0, y: 8 }}
                        animate={{ opacity: 1, y: 0 }}
                        transition={{ duration: 0.25, delay: reduce ? 0 : i * 0.06 }}
                        whileHover={reduce ? undefined : { y: -2 }}
                        className="group flex flex-col gap-3 rounded-2xl border border-border bg-surface p-6 text-left transition-colors duration-200 hover:border-[var(--accent-border)] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--accent)]"
                    >
                        <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-accent/10 text-accent">
                            <o.icon className="h-5 w-5" />
                        </span>
                        <span className="text-lg font-bold text-[var(--text)]">{o.title}</span>
                        <span className="text-sm leading-relaxed text-[var(--text-2)]">{o.desc}</span>
                        <span className="mt-2 inline-flex items-center gap-1.5 text-sm font-medium text-accent">
                            Jouer{" "}
                            <ArrowRight className="h-4 w-4 transition-transform duration-200 group-hover:translate-x-0.5" />
                        </span>
                    </motion.button>
                ))}
            </div>
        </div>
    );
}
