"use client";

import type { CSSProperties, ReactNode } from "react";

/** Carte en verre dépoli (glassmorphism) — thème Vision UI violet, tokens scopés --v-*. */
export default function VisionCard({
    children,
    className = "",
    style,
    hover = false,
    padding = 24,
}: {
    children: ReactNode;
    className?: string;
    style?: CSSProperties;
    hover?: boolean;
    padding?: number;
}) {
    return (
        <div
            className={`${hover ? "v-hover" : ""} ${className}`}
            style={{
                background: "color-mix(in srgb, var(--v-surface) 82%, transparent)",
                backdropFilter: "blur(14px)",
                WebkitBackdropFilter: "blur(14px)",
                border: "1px solid var(--v-border)",
                borderRadius: 20,
                boxShadow: "0 8px 30px rgba(0,0,0,0.25)",
                padding,
                ...style,
            }}
        >
            {children}
        </div>
    );
}
