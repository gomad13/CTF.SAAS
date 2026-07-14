"use client";

import { useState } from "react";
import { apiFetch } from "@/lib/api";
import EpreuveMode from "@/components/flashcards/EpreuveMode";
import type { Flashcard } from "@/lib/flashcards-demo";

type Content = { subtype?: string; note?: boolean; instructions?: string; cards?: Flashcard[] };

/**
 * Module « Flashcards » en mode ÉPREUVE dans un parcours.
 * Rejoue le composant EpreuveMode existant (charte violet) puis remonte le résultat
 * (bonnes réponses / total) au scoring EXISTANT `submit-flash-cards` — le serveur
 * possède l'arrondi + les points + la complétion (noté = points, non noté = 0 point).
 */
export default function EpreuveFlashCardsChallenge({
    challengeId, content, onComplete,
}: { challengeId: string; content: Content; onComplete: () => void }) {
    const cards = content.cards ?? [];
    const [submitted, setSubmitted] = useState(false);

    async function handleFinish(correct: number, total: number) {
        if (submitted) return;
        setSubmitted(true);
        try {
            // Branche "flip" du scoring flashcards existant : le serveur calcule
            // scorePercent = round(100·correct/total) puis points = round(Points·%/100).
            await apiFetch(`/api/challenges/interactive/${challengeId}/submit-flash-cards`, {
                method: "POST",
                body: JSON.stringify({ knownCount: correct, total }),
            });
        } catch { /* le résultat reste affiché ; la progression sera resynchronisée au prochain chargement */ }
    }

    if (cards.length === 0) {
        return (
            <div className="vision-dashboard">
                <div style={{ minHeight: 140, display: "flex", alignItems: "center", justifyContent: "center", textAlign: "center", fontSize: 14, color: "var(--v-text-2)", padding: 24 }}>
                    Aucune carte disponible pour cette épreuve.
                </div>
            </div>
        );
    }

    return (
        <div className="vision-dashboard" style={{ background: "transparent" }}>
            <EpreuveMode cards={cards} onExit={onComplete} onFinish={handleFinish} />
        </div>
    );
}
