"use client";

import { useState, useEffect } from "react";

export type Theme = "dark" | "light";

export function useTheme() {
    const [theme, setThemeState] = useState<Theme>("light");

    useEffect(() => {
        const saved = (localStorage.getItem("ctf_theme") as Theme | null) ?? "light";
        setThemeState(saved);
        document.documentElement.setAttribute("data-theme", saved);
    }, []);

    const setTheme = (t: Theme) => {
        setThemeState(t);
        localStorage.setItem("ctf_theme", t);
        document.documentElement.setAttribute("data-theme", t);
    };

    const toggleTheme = () => setTheme(theme === "dark" ? "light" : "dark");

    return { theme, setTheme, toggleTheme };
}
