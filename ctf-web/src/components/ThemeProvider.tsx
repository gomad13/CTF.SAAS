"use client";

import { useEffect } from "react";

export default function ThemeProvider({ children }: { children: React.ReactNode }) {
    useEffect(() => {
        const saved = localStorage.getItem("ctf_theme") ?? "dark";
        const el = document.documentElement;
        el.setAttribute("data-theme", saved);
        el.classList.toggle("dark", saved === "dark");
    }, []);

    return <>{children}</>;
}
