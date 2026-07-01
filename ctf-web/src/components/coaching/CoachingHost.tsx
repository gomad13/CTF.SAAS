"use client";

import { useEffect, useState, useCallback } from "react";
import dynamic from "next/dynamic";

// Lazy load de la modal — elle ne charge son code qu'au moment du premier
// échec, pour ne pas alourdir le bundle initial du dashboard.
const CoachingModal = dynamic(
    () => import("./CoachingModal").then((m) => ({ default: m.CoachingModal })),
    { ssr: false }
);

const EVENT_NAME = "sentys:coaching:trigger";

type TriggerDetail = { attemptId: string };

/**
 * Host global du coaching post-incident. À monter dans le layout dashboard.
 * Les composants challenge dispatchent un CustomEvent quand ils détectent
 * un échec :
 *
 *   window.dispatchEvent(new CustomEvent("sentys:coaching:trigger", {
 *     detail: { attemptId: "<challengeCompletionId>" }
 *   }));
 *
 * Ce pattern évite de toucher chaque composant challenge avec un import
 * direct + lift state — les 5 types CRI restent en lecture seule.
 */
export function CoachingHost() {
    const [openAttemptId, setOpenAttemptId] = useState<string | null>(null);

    useEffect(() => {
        const handler = (e: Event) => {
            const ce = e as CustomEvent<TriggerDetail>;
            const id = ce.detail?.attemptId;
            if (typeof id === "string" && id.length > 0) {
                setOpenAttemptId(id);
            }
        };
        window.addEventListener(EVENT_NAME, handler);
        return () => window.removeEventListener(EVENT_NAME, handler);
    }, []);

    const close = useCallback(() => setOpenAttemptId(null), []);

    if (!openAttemptId) return null;
    return <CoachingModal attemptId={openAttemptId} onClose={close} />;
}
