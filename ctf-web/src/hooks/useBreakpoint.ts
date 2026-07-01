"use client";

import { useEffect, useState } from "react";

export type Breakpoint = "mobile" | "tablet" | "desktop";

/**
 * Retourne le breakpoint courant selon le design system Sentys :
 * - "mobile"  : < 768px
 * - "tablet"  : 768px – 1023px
 * - "desktop" : ≥ 1024px
 * SSR-safe : "desktop" par défaut côté serveur, resynchronisé au montage.
 */
export function useBreakpoint(): Breakpoint {
    const [bp, setBp] = useState<Breakpoint>("desktop");

    useEffect(() => {
        if (typeof window === "undefined") return;
        const compute = (): Breakpoint => {
            const w = window.innerWidth;
            if (w < 768) return "mobile";
            if (w < 1024) return "tablet";
            return "desktop";
        };
        const onResize = () => setBp(compute());
        onResize();
        window.addEventListener("resize", onResize);
        return () => window.removeEventListener("resize", onResize);
    }, []);

    return bp;
}
