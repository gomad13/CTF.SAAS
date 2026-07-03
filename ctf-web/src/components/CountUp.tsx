"use client";

import { useEffect, useRef, useState } from "react";

/**
 * Compteur animé (count-up) sobre. Respecte prefers-reduced-motion.
 * <CountUp value={42} suffix="%" />
 */
export default function CountUp({
    value,
    suffix = "",
    duration = 900,
}: {
    value: number;
    suffix?: string;
    duration?: number;
}) {
    const [n, setN] = useState(0);
    const raf = useRef<number | undefined>(undefined);

    useEffect(() => {
        const reduce =
            typeof window !== "undefined" &&
            window.matchMedia?.("(prefers-reduced-motion: reduce)").matches;
        if (reduce || value === 0) {
            setN(value);
            return;
        }
        const start = performance.now();
        const tick = (t: number) => {
            const p = Math.min((t - start) / duration, 1);
            const eased = 1 - Math.pow(1 - p, 3); // easeOutCubic
            setN(Math.round(value * eased));
            if (p < 1) raf.current = requestAnimationFrame(tick);
        };
        raf.current = requestAnimationFrame(tick);
        return () => {
            if (raf.current) cancelAnimationFrame(raf.current);
        };
    }, [value, duration]);

    return (
        <>
            {n}
            {suffix}
        </>
    );
}
