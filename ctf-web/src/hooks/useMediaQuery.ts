"use client";

import { useEffect, useState } from "react";

/**
 * Retourne `true` quand la media query CSS passée correspond.
 * SSR-safe : renvoie `false` au premier rendu serveur puis se synchronise au montage
 * (évite tout mismatch d'hydratation). Utilisé pour les bascules de layout en JS
 * (ex. inbox liste/email, stepper wizard) quand le CSS pur ne suffit pas.
 */
export function useMediaQuery(query: string): boolean {
    const [matches, setMatches] = useState(false);

    useEffect(() => {
        if (typeof window === "undefined" || !window.matchMedia) return;
        const mql = window.matchMedia(query);
        const onChange = () => setMatches(mql.matches);
        onChange();
        mql.addEventListener("change", onChange);
        return () => mql.removeEventListener("change", onChange);
    }, [query]);

    return matches;
}

/** Raccourci : `true` sous 768px (mobile + bascule liste/détail). */
export function useIsMobile(): boolean {
    return useMediaQuery("(max-width: 767px)");
}
