"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { apiFetch } from "@/lib/api";
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

    async function handleLogout() {
        await apiFetch("/api/auth/logout", { method: "POST" }).catch(() => {});
        window.location.replace("/login");
    }

    return (
        <div className="min-h-screen bg-background-dark text-white">
            <div className="mx-auto max-w-6xl px-4 py-8">
                <div className="grid grid-cols-12 gap-4">
                    <aside className="col-span-12 md:col-span-3">
                        <div className="rounded-2xl border border-gray-200 bg-white p-4 shadow-sm text-fg-heading">
                            <div className="mb-4">
                                <div className="text-xs text-black">CTF SaaS</div>
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

                            <div className="mt-6 border-t border-gray-200 pt-4">
                                <button
                                    className="flex w-full items-center gap-2 rounded-xl border border-gray-200 bg-gray-50/40 px-3 py-2 text-sm text-black hover:bg-gray-100/40"
                                    onClick={handleLogout}
                                >
                                    <LogOut size={18} />
                                    Logout
                                </button>
                            </div>
                        </div>
                    </aside>

                    <main className="col-span-12 md:col-span-9">
                        <div className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm text-fg-heading">
                            <div className="mb-5">
                                <h1 className="text-2xl font-semibold">{title}</h1>
                                {subtitle && <p className="mt-1 text-sm text-black">{subtitle}</p>}
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
                active ? "bg-primary border border-primary text-white" : "text-black hover:bg-gray-100/40"
            )}
        >
            {icon}
            {children}
        </Link>
    );
}
