"use client";

import { useCallback, useEffect, useRef, useState } from "react";

/** Formate un nombre de secondes en mm:ss. */
export function formatMMSS(totalSeconds: number): string {
    const safe = Math.max(0, Math.floor(totalSeconds));
    const m = Math.floor(safe / 60);
    const s = safe % 60;
    return `${String(m).padStart(2, "0")}:${String(s).padStart(2, "0")}`;
}

/**
 * Chrono de session : compte les secondes tant que `running` est vrai.
 * S'arrête (conserve la valeur) quand `running` passe à faux ; `reset()` remet à 0.
 */
export function useSessionTimer(running: boolean) {
    const [seconds, setSeconds] = useState(0);
    const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

    useEffect(() => {
        if (!running) return;
        intervalRef.current = setInterval(() => setSeconds((s) => s + 1), 1000);
        return () => {
            if (intervalRef.current) clearInterval(intervalRef.current);
        };
    }, [running]);

    const reset = useCallback(() => setSeconds(0), []);

    return { seconds, label: formatMMSS(seconds), reset };
}
