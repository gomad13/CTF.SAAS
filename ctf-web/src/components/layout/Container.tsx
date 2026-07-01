import React from "react";

type ContainerWidth = "narrow" | "md" | "default" | "full";

const CLASS_BY_WIDTH: Record<ContainerWidth, string> = {
    narrow: "sentys-page-narrow", // max 480px — cartes auth, formulaires courts
    md: "sentys-page-md",         // max 760px — QCM, lecture
    default: "sentys-page",       // max 1200px — dashboards, listes
    full: "",                    // pleine largeur, padding responsive seul
};

/**
 * Conteneur de page responsive standard : largeur max centrée + marges
 * horizontales pilotées par le token --page-x (16/24/40px selon l'écran).
 * Mobile-first, aucune largeur fixe.
 */
export function Container({
    children,
    width = "default",
    className = "",
    style,
}: {
    children: React.ReactNode;
    width?: ContainerWidth;
    className?: string;
    style?: React.CSSProperties;
}) {
    const base = CLASS_BY_WIDTH[width];
    const fullPad = width === "full" ? "px-[var(--page-x)] w-full" : "";
    return (
        <div className={`${base} ${fullPad} ${className}`.trim()} style={style}>
            {children}
        </div>
    );
}

export default Container;
