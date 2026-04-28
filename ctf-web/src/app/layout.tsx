import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import Providers from "./providers";
import ThemeProvider from "@/components/ThemeProvider";
import ToastContainer from "@/components/Toast";
import BetaBanner from "@/components/BetaBanner";
import CookieBanner from "@/components/CookieBanner";

const inter = Inter({
    subsets: ["latin"],
    display: "swap",
    variable: "--font-inter",
});

const TITLE = "Viper — Formation cybersécurité immersive B2B";
const DESCRIPTION =
    "Plateforme SaaS de sensibilisation cyber : parcours immersifs, simulations de phishing, analytics par équipe. Bêta privée.";

export const metadata: Metadata = {
    title: TITLE,
    description: DESCRIPTION,
    applicationName: "Viper",
    authors: [{ name: "Viper" }],
    keywords: ["cybersécurité", "formation", "phishing", "sensibilisation", "RSSI", "B2B"],
    openGraph: {
        type: "website",
        locale: "fr_FR",
        title: TITLE,
        description: DESCRIPTION,
        siteName: "Viper",
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

export default function RootLayout({
    children,
}: Readonly<{ children: React.ReactNode }>) {
    return (
        <html lang="fr" className={inter.variable}>
            <body className={inter.className}>
                <ThemeProvider>
                    <BetaBanner />
                    <Providers>{children}</Providers>
                    <CookieBanner />
                    <ToastContainer />
                </ThemeProvider>
            </body>
        </html>
    );
}
