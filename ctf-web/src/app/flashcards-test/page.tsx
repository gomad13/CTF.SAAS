"use client";

// Route de TEST ISOLÉE — non branchée sur les parcours réels, données démo en dur.
// Aucune BDD, aucun scoring réel, aucune auth. Charte tokens uniquement.

import { useState } from "react";
import { DEMO_FLASHCARDS } from "@/lib/flashcards-demo";
import FlashcardsMenu from "@/components/flashcards/FlashcardsMenu";
import RevisionMode from "@/components/flashcards/RevisionMode";
import EpreuveMode from "@/components/flashcards/EpreuveMode";
import MemoryMode from "@/components/flashcards/MemoryMode";

type Screen = "menu" | "revision" | "epreuve" | "memory";

export default function FlashcardsTestPage() {
    const [screen, setScreen] = useState<Screen>("menu");
    const backToMenu = () => setScreen("menu");

    return (
        <main className="vision-dashboard min-h-screen px-4 py-10 sm:px-6" style={{ background: "var(--v-bg)", color: "var(--v-text)" }}>
            <div className="mx-auto w-full max-w-5xl">
                {screen === "menu" && (
                    <FlashcardsMenu total={DEMO_FLASHCARDS.length} onPick={setScreen} />
                )}
                {screen === "revision" && <RevisionMode cards={DEMO_FLASHCARDS} onExit={backToMenu} />}
                {/* key force le remount → chrono/score réinitialisés à chaque nouvelle épreuve */}
                {screen === "epreuve" && (
                    <EpreuveMode key="epreuve-run" cards={DEMO_FLASHCARDS} onExit={backToMenu} />
                )}
                {/* key force le remount → grille remélangée + chrono/coups réinitialisés */}
                {screen === "memory" && <MemoryMode key="memory-run" onExit={backToMenu} />}
            </div>
        </main>
    );
}
