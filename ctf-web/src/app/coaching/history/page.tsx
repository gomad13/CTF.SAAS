"use client";

import { useState } from "react";
import { useCoachingHistory } from "@/lib/hooks/useCoaching";
import { CoachingDetailCard } from "@/components/coaching/CoachingDetailCard";
import Reveal from "@/components/Reveal";
import { Stagger, StaggerItem } from "@/components/Stagger";

const PAGE_SIZE = 20;
const C_PRIMARY = "var(--accent)";
const C_PRIMARY_DARK = "var(--accent-hover)";

export default function CoachingHistoryPage() {
    const [page, setPage] = useState(1);
    const { data, isLoading, isError } = useCoachingHistory(page, PAGE_SIZE);

    const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1;

    return (
        <main style={{ maxWidth: 880, margin: "0 auto", padding: "32px 24px 80px" }}>
            <Reveal>
                <header className="mb-6">
                    <h1 className="text-2xl font-bold" style={{ color: "var(--text)" }}>
                        Mes coachings
                    </h1>
                    <p className="mt-1 text-sm" style={{ color: "var(--text-2)" }}>
                        Retrouve ici tous les coachings personnalisés que l&apos;IA t&apos;a livrés après tes exercices.
                    </p>
                </header>
            </Reveal>

            {isLoading && <Skeleton />}
            {isError && (
                <div className="rounded-lg p-6 text-sm" style={{ background: "var(--danger-subtle)", color: "var(--danger-t)" }}>
                    Impossible de charger ton historique pour le moment. Réessaie plus tard.
                </div>
            )}

            {data && data.items.length === 0 && (
                <div
                    className="rounded-xl border border-dashed p-8 text-center text-sm"
                    style={{ borderColor: "var(--border)", color: "var(--text-3)" }}
                >
                    Tu n&apos;as pas encore reçu de coaching. Continue à t&apos;entraîner !
                </div>
            )}

            {data && data.items.length > 0 && (
                <>
                    <Stagger className="flex flex-col gap-4" gap={0.05}>
                        {data.items.map((item) => (
                            <StaggerItem key={item.id}>
                                <CoachingDetailCard item={item} />
                            </StaggerItem>
                        ))}
                    </Stagger>

                    {totalPages > 1 && (
                        <nav className="mt-6 flex items-center justify-center gap-3" aria-label="Pagination">
                            <PageBtn label="←" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))} />
                            <span className="text-sm" style={{ color: "var(--text-2)" }}>
                                Page {page} / {totalPages}
                            </span>
                            <PageBtn label="→" disabled={page >= totalPages} onClick={() => setPage((p) => Math.min(totalPages, p + 1))} />
                        </nav>
                    )}
                </>
            )}
        </main>
    );
}

function PageBtn({ label, disabled, onClick }: { label: string; disabled: boolean; onClick: () => void }) {
    return (
        <button
            type="button"
            disabled={disabled}
            onClick={onClick}
            className="rounded-md px-3 py-1.5 text-sm font-medium text-white transition-colors duration-200 disabled:opacity-40 disabled:cursor-not-allowed"
            style={{ background: disabled ? "var(--surface-2)" : C_PRIMARY }}
            onMouseEnter={(e) => { if (!disabled) e.currentTarget.style.background = C_PRIMARY_DARK; }}
            onMouseLeave={(e) => { if (!disabled) e.currentTarget.style.background = C_PRIMARY; }}
        >
            {label}
        </button>
    );
}

function Skeleton() {
    return (
        <div className="flex flex-col gap-4">
            {[0, 1, 2].map((i) => (
                <div key={i} className="h-32 animate-pulse rounded-xl" style={{ background: "var(--surface-2)" }} />
            ))}
        </div>
    );
}
