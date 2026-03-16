"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { clearToken } from "@/lib/api";
import { cn } from "@/lib/utils";
import { LayoutDashboard, LogOut } from "lucide-react";

export function AppShell({
    title,
    subtitle,
    children,
}: {
    title: string;
    subtitle?: string;
    children: React.ReactNode;
}) {
    const pathname = usePathname();

    return (
        <div className="min-h-screen bg-neutral-950 text-neutral-100">
            <div className="mx-auto max-w-6xl px-4 py-8">
                <div className="grid grid-cols-12 gap-4">
                    <aside className="col-span-12 md:col-span-3">
                        <div className="rounded-2xl border border-neutral-800 bg-neutral-900/40 p-4 shadow-sm">
                            <div className="mb-4">
                                <div className="text-xs text-neutral-400">CTF SaaS</div>
                                <div className="text-lg font-semibold">Classic Path V1</div>
                            </div>

                            <nav className="space-y-1">
                                <NavItem
                                    href="/dashboard"
                                    active={pathname === "/dashboard"}
                                    icon={<LayoutDashboard size={18} />}
                                >
                                    Dashboard
                                </NavItem>
                            </nav>

                            <div className="mt-6 border-t border-neutral-800 pt-4">
                                <button
                                    className="flex w-full items-center gap-2 rounded-xl border border-neutral-800 bg-neutral-950/40 px-3 py-2 text-sm text-neutral-200 hover:bg-neutral-800/40"
                                    onClick={() => {
                                        clearToken();
                                        window.location.href = "/login";
                                    }}
                                >
                                    <LogOut size={18} />
                                    Logout
                                </button>
                            </div>
                        </div>
                    </aside>

                    <main className="col-span-12 md:col-span-9">
                        <div className="rounded-2xl border border-neutral-800 bg-neutral-900/40 p-5 shadow-sm">
                            <div className="mb-5">
                                <h1 className="text-2xl font-semibold">{title}</h1>
                                {subtitle && <p className="mt-1 text-sm text-neutral-400">{subtitle}</p>}
                            </div>

                            {children}
                        </div>
                    </main>
                </div>
            </div>
        </div>
    );
}

function NavItem({
    href,
    active,
    icon,
    children,
}: {
    href: string;
    active?: boolean;
    icon: React.ReactNode;
    children: React.ReactNode;
}) {
    return (
        <Link
            href={href}
            className={cn(
                "flex items-center gap-2 rounded-xl px-3 py-2 text-sm transition",
                active ? "bg-neutral-800/60 text-white" : "text-neutral-300 hover:bg-neutral-800/40"
            )}
        >
            {icon}
            {children}
        </Link>
    );
}
