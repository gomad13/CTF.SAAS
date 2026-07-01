export default function SuperAdminLayout({ children }: { children: React.ReactNode }) {
    return (
        <div style={{ minHeight: "100vh", background: "#050505", color: "#ffffff" }}>
            {children}
        </div>
    );
}
