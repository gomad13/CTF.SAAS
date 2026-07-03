import type { ReactNode } from "react";

/** Conteneur premium pour un graphique (titre + sous-titre + zone chart). */
export default function ChartCard({ title, subtitle, action, children }: {
    title: string; subtitle?: string; action?: ReactNode; children: ReactNode;
}) {
    return (
        <div className="rounded-xl border border-border bg-surface p-6 shadow-sm">
            <div className="mb-4 flex items-start justify-between gap-3">
                <div>
                    <h3 className="text-sm font-semibold" style={{ color: "var(--text)" }}>{title}</h3>
                    {subtitle && <p className="mt-0.5 text-xs" style={{ color: "var(--text-3)" }}>{subtitle}</p>}
                </div>
                {action}
            </div>
            {children}
        </div>
    );
}
