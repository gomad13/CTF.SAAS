"use client";

import { memo, useEffect, useRef } from "react";
import Link from "next/link";
import { X, Sparkles, Loader2 } from "lucide-react";
import { useGenerateCoaching } from "@/lib/hooks/useCoaching";

// Charte coaching (palette dédiée prompt P4) :
// - Primary       #03b5aa
// - Primary dark  #037971
// - Background    #023436
// - Accent        #00bfb3
const C_PRIMARY = "#03b5aa";
const C_PRIMARY_DARK = "#037971";
const C_BG = "#023436";
const C_ACCENT = "#00bfb3";

type Props = {
    /** ChallengeAttempt qui a échoué — Guid (ex: ChallengeCompletion.Id). */
    attemptId: string;
    /** Quand l'utilisateur ferme la modal ou clique "Compris !". */
    onClose: () => void;
};

function CoachingModalImpl({ attemptId, onClose }: Props) {
    const { data, error, isLoading, generate } = useGenerateCoaching();
    const triggered = useRef(false);

    useEffect(() => {
        // Garde-fou React StrictMode : on ne déclenche la génération qu'une fois,
        // même si l'effet est exécuté deux fois en dev.
        if (triggered.current) return;
        triggered.current = true;
        void generate(attemptId);
    }, [attemptId, generate]);

    // Esc pour fermer
    useEffect(() => {
        const handler = (e: KeyboardEvent) => { if (e.key === "Escape") onClose(); };
        window.addEventListener("keydown", handler);
        return () => window.removeEventListener("keydown", handler);
    }, [onClose]);

    return (
        <div
            role="dialog"
            aria-modal="true"
            aria-labelledby="coaching-title"
            className="fixed inset-0 z-[100] flex items-center justify-center px-4"
            style={{ background: "rgba(0, 0, 0, 0.8)" }}
        >
            <div
                className="relative w-full max-w-[640px] rounded-2xl p-8 shadow-2xl"
                style={{
                    background: C_BG,
                    border: `1px solid ${C_PRIMARY_DARK}`,
                    color: "#FFFFFF",
                    animation: "coachingFadeIn 220ms ease-out",
                }}
            >
                <button
                    type="button"
                    onClick={onClose}
                    aria-label="Fermer le coaching"
                    className="absolute right-3 top-3 rounded p-2 transition-colors duration-200 hover:bg-white/10"
                    style={{ color: "rgba(255,255,255,0.75)" }}
                >
                    <X size={18} strokeWidth={1.75} />
                </button>

                <header className="mb-5 flex items-center gap-3">
                    <span
                        className="flex h-9 w-9 items-center justify-center rounded-full"
                        style={{ background: `${C_ACCENT}33`, color: C_ACCENT }}
                    >
                        <Sparkles size={18} strokeWidth={1.75} />
                    </span>
                    <h2 id="coaching-title" className="text-xl font-semibold">
                        Ton coaching personnalisé
                    </h2>
                </header>

                <div className="min-h-[200px]">
                    {isLoading && <LoadingState />}
                    {!isLoading && error && <ErrorState message={error} />}
                    {!isLoading && data && <CoachingContent content={data.content} status={data.status} />}
                </div>

                <footer className="mt-6 flex items-center justify-between gap-4">
                    <Link
                        href="/coaching/history"
                        className="text-sm underline-offset-4 transition-colors duration-200 hover:underline"
                        style={{ color: C_ACCENT }}
                    >
                        Voir l&apos;historique de mes coachings
                    </Link>
                    <button
                        type="button"
                        onClick={onClose}
                        className="rounded-lg px-4 py-2 text-sm font-semibold text-white transition-colors duration-200"
                        style={{ background: C_PRIMARY }}
                        onMouseEnter={(e) => (e.currentTarget.style.background = C_PRIMARY_DARK)}
                        onMouseLeave={(e) => (e.currentTarget.style.background = C_PRIMARY)}
                    >
                        Compris !
                    </button>
                </footer>
            </div>

            <style jsx>{`
                @keyframes coachingFadeIn {
                    from { opacity: 0; transform: scale(0.97); }
                    to   { opacity: 1; transform: scale(1); }
                }
            `}</style>
        </div>
    );
}

function LoadingState() {
    return (
        <div className="flex h-[200px] flex-col items-center justify-center gap-3 text-center">
            <Loader2 size={32} strokeWidth={1.5} className="animate-spin" style={{ color: C_PRIMARY }} />
            <p className="text-base font-medium" style={{ color: "rgba(255,255,255,0.92)" }}>
                Analyse de ton parcours en cours...
            </p>
            <p className="text-sm" style={{ color: "rgba(255,255,255,0.6)" }}>
                Le coaching peut prendre quelques secondes — c&apos;est généré sur ta machine.
            </p>
        </div>
    );
}

function ErrorState({ message }: { message: string }) {
    return (
        <div className="flex h-[200px] flex-col items-center justify-center gap-2 text-center">
            <p className="text-base font-medium text-white">Coaching temporairement indisponible</p>
            <p className="text-sm" style={{ color: "rgba(255,255,255,0.65)" }}>{message}</p>
        </div>
    );
}

function CoachingContent({ content, status }: { content: string; status: string }) {
    return (
        <div>
            {status === "Fallback" && (
                <p className="mb-3 text-xs" style={{ color: "rgba(255,255,255,0.55)" }}>
                    Coaching basé sur un modèle générique — l&apos;IA locale était indisponible.
                </p>
            )}
            <div
                className="whitespace-pre-line text-base leading-relaxed"
                style={{ color: "rgba(255,255,255,0.92)" }}
            >
                {content}
            </div>
        </div>
    );
}

export const CoachingModal = memo(CoachingModalImpl);
