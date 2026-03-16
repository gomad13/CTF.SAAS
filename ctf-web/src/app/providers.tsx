import "./globals.css";
import Providers from "./providers";

export const metadata = {
    title: "CTF SaaS",
    description: "CTF training platform",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
    return (
        <html lang="fr">
            <body className="min-h-screen bg-neutral-950 text-neutral-100">
                <Providers>{children}</Providers>
            </body>
        </html>
    );
}
