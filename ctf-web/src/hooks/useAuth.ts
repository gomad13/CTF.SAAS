"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "";

export interface AuthUser {
    id: string;
    email: string;
    role: string;
    firstName: string;
    lastName: string;
    tenantId: string;
    tenantName?: string;
}

export function useAuth() {
    const [user, setUser]       = useState<AuthUser | null>(null);
    const [loading, setLoading] = useState(true);
    const router                = useRouter();

    useEffect(() => {
        fetch(`${API_BASE}/api/auth/me`, { credentials: "include" })
            .then(r => (r.ok ? r.json() : null))
            .then(data => { setUser(data); setLoading(false); })
            .catch(() => { setUser(null); setLoading(false); });
    }, []);

    const logout = async () => {
        try {
            await fetch(`${API_BASE}/api/auth/logout`, {
                method: "POST",
                credentials: "include",
                headers: { "X-Requested-With": "XMLHttpRequest" },
            });
        } finally {
            setUser(null);
            router.push("/login");
        }
    };

    const isAdmin = user?.role?.toLowerCase() === "admin";
    const isUser  = user?.role?.toLowerCase() === "user" || isAdmin;

    return { user, loading, logout, isAdmin, isUser };
}
