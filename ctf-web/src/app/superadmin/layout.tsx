export default function SuperAdminLayout({ children }: { children: React.ReactNode }) {
    return (
        <div style={{ minHeight: "100vh", background: "var(--bg)", color: "var(--text)" }}>
            {children}
        </div>
    );
}
