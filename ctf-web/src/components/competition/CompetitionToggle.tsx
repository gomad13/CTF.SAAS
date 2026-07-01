"use client";

import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Trophy, X } from "lucide-react";
import { apiFetch } from "@/lib/api";
import { useCompetitionStatus } from "@/hooks/useCompetitionStatus";

export default function CompetitionToggle() {
    const qc = useQueryClient();
    const statusQ = useCompetitionStatus();
    const [showConfirm, setShowConfirm] = useState<null | boolean>(null);
    const [toast, setToast] = useState<string | null>(null);

    const isEnabled = statusQ.data?.isEnabled === true;

    const toggleM = useMutation({
        mutationFn: (next: boolean) =>
            apiFetch<{ isEnabled: boolean }>("/api/admin/competition/toggle", {
                method: "PATCH",
                body: JSON.stringify({ isEnabled: next }),
            }),
        onSuccess: (res) => {
            qc.setQueryData(["competition", "status"], { isEnabled: res.isEnabled });
            qc.invalidateQueries({ queryKey: ["competition"] });
            setToast(res.isEnabled ? "Mode Compétition activé" : "Mode Compétition désactivé");
            setShowConfirm(null);
            setTimeout(() => setToast(null), 3500);
        },
        onError: (err: Error) => {
            setToast(err.message || "Échec de la mise à jour");
            setShowConfirm(null);
            setTimeout(() => setToast(null), 4000);
        },
    });

    return (
        <div className="rounded-xl border border-border bg-surface p-6 shadow-sm">
            <div className="flex items-start justify-between gap-4">
                <div className="flex items-start gap-3">
                    <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary">
                        <Trophy size={18} strokeWidth={2} />
                    </div>
                    <div>
                        <h3 className="text-base font-semibold text-fg-heading">Mode Compétition</h3>
                        <p className="mt-1 text-sm text-fg-body">
                            Active un classement global pour votre organisation (podium, scoreboard).
                            Les utilisateurs voient l&apos;onglet « Compétition » dans la navigation.
                        </p>
                    </div>
                </div>
                <label className="inline-flex shrink-0 items-center" aria-label="Activer le mode compétition">
                    <input
                        type="checkbox"
                        className="peer sr-only"
                        checked={isEnabled}
                        disabled={statusQ.isLoading || toggleM.isPending}
                        onChange={e => setShowConfirm(e.target.checked)}
                    />
                    <span className="relative h-6 w-11 rounded-full bg-border transition-colors duration-200 peer-checked:bg-primary peer-disabled:opacity-50">
                        <span className="absolute left-0.5 top-0.5 h-5 w-5 rounded-full bg-white shadow-sm transition-transform duration-200 peer-checked:translate-x-5" />
                    </span>
                </label>
            </div>

            {showConfirm !== null && (
                <div className="fixed inset-0 z-50 flex items-center justify-center bg-sidebar/60 backdrop-blur-sm">
                    <div className="mx-4 w-full max-w-md rounded-xl border border-border bg-surface p-6 shadow-lg">
                        <div className="flex items-start justify-between gap-3">
                            <h4 className="text-base font-semibold text-fg-heading">
                                {showConfirm ? "Activer le mode Compétition ?" : "Désactiver le mode Compétition ?"}
                            </h4>
                            <button
                                type="button"
                                onClick={() => setShowConfirm(null)}
                                className="text-fg-muted transition-colors duration-200 hover:text-fg-heading"
                            >
                                <X size={16} />
                            </button>
                        </div>
                        <p className="mt-2 text-sm leading-relaxed text-fg-body">
                            {showConfirm
                                ? "Tous les utilisateurs de votre organisation verront apparaître l'onglet Compétition et auront accès au classement."
                                : "L'onglet Compétition disparaîtra pour tous les utilisateurs. Les scores restent conservés en base."}
                        </p>
                        <div className="mt-5 flex justify-end gap-2">
                            <button
                                type="button"
                                onClick={() => setShowConfirm(null)}
                                className="rounded-lg border border-border bg-surface px-4 py-2 text-sm font-medium text-fg-body transition-colors duration-200 hover:bg-table-head"
                            >
                                Annuler
                            </button>
                            <button
                                type="button"
                                onClick={() => toggleM.mutate(showConfirm)}
                                disabled={toggleM.isPending}
                                className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white transition-colors duration-200 hover:bg-primary-hover disabled:opacity-60"
                            >
                                {toggleM.isPending ? "En cours…" : "Confirmer"}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {toast && (
                <div className="fixed bottom-6 right-6 z-50 rounded-lg border border-border bg-surface px-4 py-3 text-sm font-medium text-fg-heading shadow-lg">
                    {toast}
                </div>
            )}
        </div>
    );
}
