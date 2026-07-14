"use client";

import { useState } from "react";
import { apiFetch } from "@/lib/api";
import MemoryMode from "@/components/flashcards/MemoryMode";

/**
 * Module « Flashcards — Memory » dans un parcours (jeu d'association).
 * Rejoue le MemoryMode existant ; à la fin (toutes paires trouvées), remonte la complétion
 * au scoring EXISTANT `submit-flash-cards`. Noté = points ; non noté = 0 point (complète l'étape).
 */
export default function MemoryChallenge({
    challengeId, onComplete,
}: { challengeId: string; onComplete: () => void }) {
    const [submitted, setSubmitted] = useState(false);

    async function handleFinish() {
        if (submitted) return;
        setSubmitted(true);
        try {
            // Memory terminé = 100 % de réussite ; le serveur applique le barème du challenge.
            await apiFetch(`/api/challenges/interactive/${challengeId}/submit-flash-cards`, {
                method: "POST",
                body: JSON.stringify({ knownCount: 1, total: 1 }),
            });
        } catch { /* la progression sera resynchronisée au prochain chargement */ }
    }

    return (
        <div className="vision-dashboard" style={{ background: "transparent" }}>
            <MemoryMode onExit={onComplete} onFinish={handleFinish} continueLabel="Continuer le parcours" />
        </div>
    );
}
