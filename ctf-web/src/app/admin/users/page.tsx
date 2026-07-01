"use client";
import { useMemo, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import { RequireAuth } from "@/components/RequireAuth";
import type { AdminPathListItemDto } from "@/lib/adminTypes";

type ImportUsersResult = {
    created: number;
    updated: number;
    skipped: number;
    errors: string[];
};

export default function AdminUsersImportPage() {
    const [file, setFile] = useState<File | null>(null);
    const [selectedPathId, setSelectedPathId] = useState<string>("");

    const pathsQuery = useQuery({
        queryKey: ["admin-paths"],
        queryFn: () => apiFetch<AdminPathListItemDto[]>("/api/admin/paths"),
    });

    const selectedPathLabel = useMemo(() => {
        const p = pathsQuery.data?.find((x) => x.id === selectedPathId);
        return p ? p.title : "";
    }, [pathsQuery.data, selectedPathId]);

    const importMutation = useMutation({
        mutationFn: async () => {
            if (!file) throw new Error("Choisis un fichier CSV");
            const form = new FormData();
            form.append("file", file);
            const qs = selectedPathId ? `?autoAssignPathId=${selectedPathId}` : "";
            return apiFetch<ImportUsersResult>(`/api/admin/users/import${qs}`, {
                method: "POST",
                body: form,
            });
        },
    });

    return (
        <RequireAuth>
            <div className="max-w-4xl mx-auto p-4 sm:p-8 space-y-6">
                <div className="rounded-2xl border border-neutral-800 bg-background-dark p-4">
                    <div className="text-lg font-semibold">Import employés (CSV)</div>
                    <div className="mt-1 text-sm text-neutral-300">
                        Format attendu (virgule ou point-virgule) :{" "}
                        <span className="text-neutral-300">lastName;firstName;email</span>
                    </div>
                </div>

                <div className="space-y-4 rounded-2xl border border-neutral-800 bg-background-dark p-4">
                    <div className="grid gap-3 md:grid-cols-2">
                        <div>
                            <label className="text-xs text-neutral-300">Fichier CSV</label>
                            <input
                                type="file"
                                accept=".csv"
                                onChange={(e) => setFile(e.target.files?.[0] ?? null)}
                                className="mt-1 block w-full rounded-xl border border-neutral-800 bg-background-dark px-3 py-2 text-sm"
                            />
                            <div className="mt-1 text-xs text-neutral-300">
                                Astuce : Excel FR exporte souvent en <b>;</b> — est supporté
                            </div>
                        </div>
                        <div>
                            <label className="text-xs text-neutral-300">
                                Auto-assign formation (optionnel)
                            </label>
                            <select
                                value={selectedPathId}
                                onChange={(e) => setSelectedPathId(e.target.value)}
                                className="mt-1 w-full rounded-xl border border-neutral-800 bg-background-dark px-3 py-2 text-sm"
                            >
                                <option value="">
                                    {pathsQuery.isLoading
                                        ? "Chargement des formations..."
                                        : "Aucune (laisser sans assignation)"}
                                </option>
                                {pathsQuery.data?.map((p) => (
                                    <option key={p.id} value={p.id}>
                                        {p.title} {p.publishedAt ? "" : "(non publié)"}
                                    </option>
                                ))}
                            </select>
                            <div className="mt-1 text-xs text-neutral-300">
                                {selectedPathId
                                    ? `Les employés importés seront assignés : ${selectedPathLabel}`
                                    : "Si tu ne choisis rien, on importe juste les utilisateurs (sans assignment)."}
                            </div>
                        </div>
                    </div>

                    <button
                        onClick={() => importMutation.mutate()}
                        className="rounded-xl border border-neutral-800 px-4 py-2 text-sm hover:bg-neutral-900 disabled:opacity-50"
                        disabled={importMutation.isPending || !file}
                    >
                        {importMutation.isPending ? "Import en cours..." : "Importer"}
                    </button>

                    {importMutation.data && (
                        <div className="rounded-xl border border-neutral-800 bg-background-dark p-3 text-sm">
                            <div>✅ Created: {importMutation.data.created}</div>
                            <div>🔄 Updated: {importMutation.data.updated}</div>
                            <div>⏭ Skipped: {importMutation.data.skipped}</div>
                            {importMutation.data.errors?.length > 0 && (
                                <div className="mt-2 text-red-300">
                                    <div className="font-semibold">Erreurs :</div>
                                    <ul className="list-disc pl-5">
                                        {importMutation.data.errors.slice(0, 20).map((e, i) => (
                                            <li key={i}>{e}</li>
                                        ))}
                                    </ul>
                                </div>
                            )}
                        </div>
                    )}

                    {importMutation.error && (
                        <div className="text-sm text-red-300">
                            Erreur : {(importMutation.error as Error).message}
                        </div>
                    )}
                </div>
            </div>
        </RequireAuth>
    );
}