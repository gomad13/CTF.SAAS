"use client";

import { useState, useEffect } from "react";

export type Theme = "dark" | "light";

function applyTheme(t: Theme) {
    const el = document.documentElement;
    el.setAttribute("data-theme", t);
    el.classList.toggle("dark", t === "dark");
}

export function useTheme() {
    const [theme, setThemeState] = useState<Theme>("dark");

    useEffect(() => {
        const saved = (localStorage.getItem("ctf_theme") as Theme | null) ?? "dark";
        setThemeState(saved);
        applyTheme(saved);
    }, []);

    const setTheme = (t: Theme) => {
        setThemeState(t);
        localStorage.setItem("ctf_theme", t);
        applyTheme(t);
    };

    const toggleTheme = () => setTheme(theme === "dark" ? "light" : "dark");

    return { theme, setTheme, toggleTheme };
}
