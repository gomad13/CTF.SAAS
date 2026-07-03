"use client";

import Link from "next/link";
import { Calendar, CheckCircle2, Clock, FolderOpen, Mail } from "lucide-react";
import { useMyCampaigns } from "@/lib/hooks/useCampaigns";
import { STATUS_STYLES, type EmployeeCampaign, type EmployeeCampaignContent } from "@/lib/types/campaigns";

export default function MyCampaignsPage() {
    const { data, isLoading, isError } = useMyCampaigns();

    if (isLoading) return <div className="px-6 py-12 text-center text-fg-muted">Chargement…</div>;
    if (isError) {
        return (
            <div className="mx-auto max-w-3xl px-6 py-12 text-center text-sm text-[#EF4444]">
                Erreur de chargement des campagnes.
            </div>
        );
    }

    const items = data ?? [];

    return (
        <div className="mx-auto flex max-w-5xl flex-col gap-6 px-4 py-6 sm:px-6 sm:py-8">
            <div>
                <h1 className="text-2xl font-bold text-[#F1F5F9]">Mes campagnes</h1>
                <p className="mt-1 text-sm text-fg-muted">
                    Programmes de sensibilisation qui vous sont assignés et votre progression.
                </p>
            </div>

            {items.length === 0 ? (
                <div className="rounded-xl border border-[#E2E8F0] bg-surface p-8 text-center text-fg-heading shadow-sm">
                    <div className="mx-auto mb-3 inline-flex h-12 w-12 items-center justify-center rounded-full bg-[#3B82F6]/10 text-[#3B82F6]">
                        <Calendar size={22} />
                    </div>
                    <p className="text-sm text-fg-body">Aucune campagne ne t&apos;est assignée pour le moment.</p>
                </div>
            ) : (
                <div className="flex flex-col gap-4">
                    {items.map(c => <EmployeeCampaignCard key={c.campaignId} c={c} />)}
                </div>
            )}
        </div>
    );
}

function EmployeeCampaignCard({ c }: { c: EmployeeCampaign }) {
    const s = STATUS_STYLES[c.status];
    const pct = Math.max(0, Math.min(100, Math.round(c.myCompletionPercentage)));
    const remainingMs = new Date(c.endDate).getTime() - Date.now();
    const remainingDays = Math.max(0, Math.ceil(remainingMs / (1000 * 60 * 60 * 24)));

    return (
        <section className="rounded-xl border border-[#E2E8F0] bg-surface p-6 text-fg-heading shadow-sm">
            <div className="flex items-start justify-between gap-3">
                <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                        <h2 className="truncate text-lg font-semibold text-fg-heading">{c.name}</h2>
                        <span
                            className="rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase"
                            style={{ background: s.bg, color: s.color, border: `1px solid ${s.border}` }}
                        >
                            {s.label}
                        </span>
                    </div>
                    {c.description && <p className="mt-1 text-sm text-fg-body">{c.description}</p>}
                    <div className="mt-2 flex flex-wrap items-center gap-x-4 text-xs text-fg-muted">
                        <span className="inline-flex items-center gap-1"><Calendar size={11} />Fin le {new Date(c.endDate).toLocaleDateString("fr-FR")}</span>
                        {c.status === "Active" && remainingDays > 0 && (
                            <span className="inline-flex items-center gap-1 text-[#92400E]">
                                <Clock size={11} /> Il reste {remainingDays} jour{remainingDays > 1 ? "s" : ""}
                            </span>
                        )}
                    </div>
                </div>
            </div>

            <div className="mt-4 flex items-center gap-3">
                <div className="h-2 flex-1 overflow-hidden rounded-full bg-[#E2E8F0]">
                    <div className="h-full rounded-full bg-[#3B82F6] transition-all" style={{ width: `${pct}%` }} />
                </div>
                <span className="text-sm font-semibold text-fg-heading">{pct}%</span>
            </div>

            <ul className="mt-4 flex flex-col gap-2">
                {c.contents.map(content => <ContentRow key={content.campaignContentId} c={content} />)}
            </ul>
        </section>
    );
}

function ContentRow({ c }: { c: EmployeeCampaignContent }) {
    const isCompleted = c.status === "Completed";
    const isParcours = c.contentType === "Parcours";
    // Lien vers la ressource d'origine si possible
    const href = isParcours ? `/dashboard/parcours/${c.contentId}` : `/inbox`;

    return (
        <li className="flex items-center gap-3 rounded-lg border border-[#E2E8F0] bg-[#F8FAFC] px-3 py-2">
            <span
                className="inline-flex h-7 w-7 items-center justify-center rounded-full"
                style={{
                    background: isParcours ? "rgba(59,130,246,0.10)" : "rgba(16,185,129,0.10)",
                    color: isParcours ? "#1E40AF" : "#065F46",
                }}
            >
                {isParcours ? <FolderOpen size={13} /> : <Mail size={13} />}
            </span>
            <div className="min-w-0 flex-1">
                <div className="truncate text-sm font-medium text-fg-heading">{c.title}</div>
                <div className="text-[11px] text-fg-muted">
                    {isParcours ? "Parcours" : "Scénario"} ·{" "}
                    {c.status === "NotStarted" && "Pas commencé"}
                    {c.status === "InProgress" && "En cours"}
                    {c.status === "Completed" && "Terminé"}
                    {c.status === "Failed" && "Échec"}
                    {c.completionPercentage != null && c.status !== "NotStarted" && ` — ${Math.round(c.completionPercentage)}%`}
                </div>
            </div>
            {isCompleted ? (
                <CheckCircle2 size={16} className="text-[#10B981]" />
            ) : (
                <Link href={href} className="text-xs font-medium text-[#3B82F6] hover:text-[#2563EB]">
                    {c.status === "NotStarted" ? "Démarrer" : "Continuer"} →
                </Link>
            )}
        </li>
    );
}
