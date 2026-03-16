"use client";

import { useState } from "react";
import { apiFetch, setToken } from "@/lib/api";

export default function LoginPage() {
    const [tenantId, setTenantId] = useState("11111111-1111-1111-1111-111111111111");
    const [userId, setUserId] = useState("");
    const [role, setRole] = useState<"admin" | "user">("user");
    const [error, setError] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);

    async function onSubmit(e: React.FormEvent) {
        e.preventDefault();
        setError(null);
        setLoading(true);

        try {
            const res = await apiFetch<{ token: string }>("/api/auth/dev-token", {
                method: "POST",
                body: JSON.stringify({ tenantId, userId, role }),
                auth: false,
            });

            setToken(res.token);
            window.location.href = "/dashboard";
        } catch (err: any) {
            setError(err.message || "Login failed");
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="min-h-screen bg-neutral-950 text-neutral-100">
            <div className="mx-auto flex min-h-screen max-w-xl items-center px-4">
                <div className="w-full rounded-2xl border border-neutral-800 bg-neutral-900/40 p-6 shadow-sm">
                    <h1 className="text-2xl font-semibold">Connexion (DEV)</h1>
                    <p className="mt-1 text-sm text-neutral-400">
                        Génère un token via <code>/api/auth/dev-token</code>.
                    </p>

                    <form className="mt-6 space-y-3" onSubmit={onSubmit}>
                        <Field label="TenantId" value={tenantId} onChange={setTenantId} />
                        <Field label="UserId" value={userId} onChange={setUserId} placeholder="GUID user (table users)" />

                        <div>
                            <label className="mb-1 block text-sm text-neutral-300">Role</label>
                            <select
                                className="w-full rounded-xl border border-neutral-800 bg-neutral-950/40 px-3 py-2 text-sm"
                                value={role}
                                onChange={(e) => setRole(e.target.value as any)}
                            >
                                <option value="user">user</option>
                                <option value="admin">admin</option>
                            </select>
                        </div>

                        {error && (
                            <div className="rounded-xl border border-red-900/60 bg-red-950/30 px-3 py-2 text-sm text-red-200">
                                {error}
                            </div>
                        )}

                        <button
                            disabled={loading}
                            className="w-full rounded-xl bg-white px-3 py-2 text-sm font-semibold text-neutral-950 hover:opacity-90 disabled:opacity-60"
                        >
                            {loading ? "Connexion..." : "Se connecter"}
                        </button>
                    </form>
                </div>
            </div>
        </div>
    );
}

function Field({
    label,
    value,
    onChange,
    placeholder,
}: {
    label: string;
    value: string;
    onChange: (v: string) => void;
    placeholder?: string;
}) {
    return (
        <div>
            <label className="mb-1 block text-sm text-neutral-300">{label}</label>
            <input
                className="w-full rounded-xl border border-neutral-800 bg-neutral-950/40 px-3 py-2 text-sm"
                value={value}
                placeholder={placeholder}
                onChange={(e) => onChange(e.target.value)}
            />
        </div>
    );
}
