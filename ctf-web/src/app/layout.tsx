import type { Metadata, Viewport } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import Providers from "./providers";
import ThemeProvider from "@/components/ThemeProvider";
import ToastContainer from "@/components/Toast";
import BetaBanner from "@/components/BetaBanner";
import CookieBanner from "@/components/CookieBanner";
import ConsentGate from "@/components/legal/ConsentGate";

const inter = Inter({
    subsets: ["latin"],
    display: "swap",
    variable: "--font-inter",
});

const TITLE = "Sentys — Formation cybersécurité immersive B2B";
const DESCRIPTION =
    "Plateforme SaaS de sensibilisation cyber : parcours immersifs, simulations de phishing, analytics par équipe. Bêta privée.";

export const metadata: Metadata = {
    title: TITLE,
    description: DESCRIPTION,
    applicationName: "Sentys",
    authors: [{ name: "Sentys" }],
    keywords: ["cybersécurité", "formation", "phishing", "sensibilisation", "RSSI", "B2B"],
    openGraph: {
        type: "website",
        locale: "fr_FR",
        title: TITLE,
        description: DESCRIPTION,
        siteName: "Sentys",
    },
    twitter: {
        card: "summary_large_image",
        title: TITLE,
        description: DESCRIPTION,
    },
    robots: {
        // Bêta privée : on ne veut pas être indexé tant que le contenu n'est pas finalisé.
        index: false,
        follow: false,
    },
};

// Meta viewport explicite (mobile-first). Sans ça, certains mobiles affichent
// la page en mode desktop dézoomé. initialScale=1, l'utilisateur garde le zoom.
export const viewport: Viewport = {
    width: "device-width",
    initialScale: 1,
    viewportFit: "cover",
};

export default function RootLayout({
    children,
}: Readonly<{ children: React.ReactNode }>) {
    return (
        <html lang="fr" className={inter.variable}>
            <body className={inter.className}>
                <ThemeProvider>
                    <BetaBanner />
                    <Providers>
                        {children}
                        <ConsentGate />
                    </Providers>
                    <CookieBanner />
                    <ToastContainer />
                </ThemeProvider>
            </body>
        </html>
    );
}
