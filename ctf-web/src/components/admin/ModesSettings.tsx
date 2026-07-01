"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Trophy, BarChart3, CheckCircle2, Users, Target, X } from "lucide-react";
import { apiFetch } from "@/lib/api";

type ModeKey = "competition" | "analytics" | "compliance" | "teams" | "campaigns";

type AllModesStatus = Record<ModeKey, boolean>;

type ModeConfig = {
    key: ModeKey;
    title: string;
    description: string;
    icon: typeof Trophy;
    statusPath: string;
    togglePath: string;
};

const MODES: ModeConfig[] = [
    {
        key: "competition",
        title: "Mode Compétition",
        description: "Active un classement global (podium, scoreboard) pour votre organisation.",
        icon: Trophy,
        statusPath: "/api/competition/status",
        togglePath: "/api/admin/competition/toggle",
    },
    {
        key: "analytics",
        title: "Analytics avancés",
        description: "Dashboard détaillé d'usage + exports pour les admins.",
        icon: BarChart3,
        statusPath: "/api/analytics/status",
        togglePath: "/api/admin/analytics/toggle",
    },
    {
        key: "compliance",
        title: "Formation obligatoire",
        description: "Parcours obligatoires avec deadline et suivi de la compliance.",
        icon: CheckCircle2,
        statusPath: "/api/compliance/status",
        togglePath: "/api/admin/compliance/toggle",
    },
    {
        key: "teams",
        title: "Équipes & Départements",
        description: "Segmentez vos collaborateurs en équipes pour assignation et comparaison ciblées.",
        icon: Users,
        statusPath: "/api/teams/status",
        togglePath: "/api/admin/teams/toggle",
    },
    {
        key: "campaigns",
        title: "Campagnes",
        description: "Campagnes de formation time-boxed avec tracking dédié.",
        icon: Target,
        statusPath: "/api/campaigns/status",
        togglePath: "/api/admin/campaigns/toggle",
    },
];

export default function ModesSettings() {
    const qc = useQueryClient();
    const statusQ = useQuery<AllModesStatus>({
        queryKey: ["modes", "all"],
        queryFn: () => apiFetch<AllModesStatus>("/api/modes/all"),
        staleTime: 15_000,
    });

    const [pending, setPending] = useState<{ mode: ModeConfig; nextValue: boolean } | null>(null);
    const [toast, setToast] = useState<string | null>(null);

    const toggleM = useMutation({
        mutationFn: async (p: { mode: ModeConfig; value: boolean }) => {
            await apiFetch(p.mode.togglePath, {
                method: "PATCH",
                body: JSON.stringify({ isEnabled: p.value }),
            });
            return p;
        },
        onSuccess: ({ mode, value }) => {
            qc.setQueryData<AllModesStatus>(["modes", "all"], old => ({
                ...(old ?? ({} as AllModesStatus)),
                [mode.key]: value,
            } as AllModesStatus));
            qc.invalidateQueries({ queryKey: ["modes"] });
            qc.invalidateQueries({ queryKey: [mode.key] });
            setToast(`${mode.title} ${value ? "activé" : "désactivé"}`);
            setPending(null);
            setTimeout(() => setToast(null), 3500);
        },
        onError: (err: Error) => {
            setToast(err.message || "Échec de la mise à jour");
            setPending(null);
            setTimeout(() => setToast(null), 4000);
        },
    });

    const isEnabled = (key: ModeKey) => statusQ.data?.[key] === true;

    return (
        <div className="flex flex-col gap-3">
            <h2 className="text-sm font-semibold uppercase tracking-wider text-[#94A3B8]">Modes entreprise</h2>
            <div className="overflow-hidden rounded-xl border border-[#E2E8F0] bg-white shadow-sm">
                {MODES.map((m, idx) => {
                    const enabled = isEnabled(m.key);
                    const Icon = m.icon;
                    return (
                        <div
                            key={m.key}
                            className={`flex items-start justify-between gap-4 px-4 py-4 sm:px-6 sm:py-5 ${idx > 0 ? "border-t border-[#E2E8F0]" : ""}`}
                        >
                            <div className="flex items-start gap-3">
                                <div
                                    className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg"
                                    style={{ background: "rgba(59,130,246,0.10)", color: "#1E40AF" }}
                                >
                                    <Icon size={18} strokeWidth={2} />
                                </div>
                                <div>
                                    <h3 className="text-base font-semibold text-[#1E293B]">{m.title}</h3>
                                    <p className="mt-1 text-sm text-[#334155]">{m.description}</p>
                                </div>
                            </div>
                            <label className="inline-flex shrink-0 items-center">
                                <input
                                    type="checkbox"
                                    className="peer sr-only"
                                    checked={enabled}
                                    disabled={statusQ.isLoading || toggleM.isPending}
                                    onChange={e => setPending({ mode: m, nextValue: e.target.checked })}
                                />
                                <span className="relative h-6 w-11 rounded-full bg-[#E2E8F0] transition-colors duration-200 peer-checked:bg-[#3B82F6] peer-disabled:opacity-50">
                                    <span className="absolute left-0.5 top-0.5 h-5 w-5 rounded-full bg-white shadow-sm transition-transform duration-200 peer-checked:translate-x-5" />
                                </span>
                            </label>
                        </div>
                    );
                })}
            </div>

            {pending && (
                <div className="fixed inset-0 z-50 flex items-center justify-center bg-[#0F172A]/60 backdrop-blur-sm">
                    <div className="mx-4 w-full max-w-md rounded-xl border border-[#E2E8F0] bg-white p-6 shadow-lg">
                        <div className="flex items-start justify-between gap-3">
                            <h4 className="text-base font-semibold text-[#1E293B]">
                                {pending.nextValue ? `Activer ${pending.mode.title} ?` : `Désactiver ${pending.mode.title} ?`}
                            </h4>
                            <button
                                type="button"
                                onClick={() => setPending(null)}
                                className="text-[#64748B] transition-colors duration-200 hover:text-[#1E293B]"
                            >
                                <X size={16} />
                            </button>
                        </div>
                        <p className="mt-2 text-sm leading-relaxed text-[#334155]">
                            {pending.nextValue
                                ? "Tous les utilisateurs de votre organisation seront concernés immédiatement."
                                : "Le mode sera désactivé pour tous les utilisateurs. Les données restent conservées en base."}
                        </p>
                        <div className="mt-5 flex justify-end gap-2">
                            <button
                                type="button"
                                onClick={() => setPending(null)}
                                className="rounded-lg border border-[#E2E8F0] bg-white px-4 py-2 text-sm font-medium text-[#334155] transition-colors duration-200 hover:bg-[#F1F5F9]"
                            >
                                Annuler
                            </button>
                            <button
                                type="button"
                                onClick={() => toggleM.mutate({ mode: pending.mode, value: pending.nextValue })}
                                disabled={toggleM.isPending}
                                className="rounded-lg bg-[#3B82F6] px-4 py-2 text-sm font-medium text-white transition-colors duration-200 hover:bg-[#2563EB] disabled:opacity-60"
                            >
                                {toggleM.isPending ? "En cours…" : "Confirmer"}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {toast && (
                <div className="fixed bottom-6 right-6 z-50 rounded-lg border border-[#E2E8F0] bg-white px-4 py-3 text-sm font-medium text-[#1E293B] shadow-lg">
                    {toast}
                </div>
            )}
        </div>
    );
}
