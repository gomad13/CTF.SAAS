"use client";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { apiFetch } from "@/lib/api";

export function RequireAuth({ children }: { children: React.ReactNode }) {
    const router = useRouter();
    const [ready, setReady] = useState(false);

    useEffect(() => {
        apiFetch("/api/assignments/mine")
            .then(() => setReady(true))
            .catch(() => router.replace("/login"));
    }, [router]);

    if (!ready) return (
        <div className="flex min-h-screen items-center justify-center bg-background-dark">
            <div className="h-8 w-8 animate-spin rounded-full border-2 border-primary border-t-transparent" />
        </div>
    );

    return <>{children}</>;
}
