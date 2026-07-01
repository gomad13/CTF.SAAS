import React from "react";

/**
 * En-tête de page responsive : titre (taille fluide via --fs-h1) + sous-titre
 * optionnel + zone d'actions qui passe sous le titre sur mobile.
 */
export function PageHeader({
    title,
    subtitle,
    actions,
    icon,
}: {
    title: string;
    subtitle?: string;
    actions?: React.ReactNode;
    icon?: React.ReactNode;
}) {
    return (
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex items-center gap-3 min-w-0">
                {icon && <span className="flex shrink-0">{icon}</span>}
                <div className="min-w-0">
                    <h1
                        className="font-bold text-fg-heading truncate"
                        style={{ fontSize: "var(--fs-h1)", lineHeight: 1.2 }}
                    >
                        {title}
                    </h1>
                    {subtitle && (
                        <p className="mt-1 text-sm font-medium text-fg-muted">{subtitle}</p>
                    )}
                </div>
            </div>
            {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
        </div>
    );
}

export default PageHeader;
