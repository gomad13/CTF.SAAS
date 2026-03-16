import Link from "next/link";

export default function AdminLayout({ children }: { children: React.ReactNode }) {
    return (
        <div className="min-h-screen bg-neutral-950 text-neutral-100">
            <div className="flex">
                {/* Sidebar */}
                <aside className="w-64 border-r border-neutral-800 p-4">
                    <div className="text-xl font-semibold">CTF Admin</div>
                    <div className="mt-6 space-y-2">
                        <Link className="block rounded-lg px-3 py-2 hover:bg-neutral-900" href="/admin">
                            Dashboard
                        </Link>
                        <Link className="block rounded-lg px-3 py-2 hover:bg-neutral-900" href="/admin/users">
                            Import employés
                        </Link>
                    </div>
                </aside>

                {/* Main */}
                <main className="flex-1">
                    <header className="flex items-center justify-between border-b border-neutral-800 px-6 py-4">
                        <div className="text-sm text-neutral-400">Entreprise (tenant) — Admin panel</div>
                        <div className="text-sm text-neutral-300">Role: admin</div>
                    </header>

                    <div className="p-6">{children}</div>
                </main>
            </div>
        </div>
    );
}
