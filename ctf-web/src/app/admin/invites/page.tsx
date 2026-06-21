"use client";

import InvitesManager from "@/components/invites/InvitesManager";

export default function InvitesPage() {
    return (
        <div className="mx-auto flex max-w-5xl flex-col gap-6 px-4 py-6 sm:px-6 sm:py-8">
            <div>
                <h1 className="text-2xl font-bold text-[#F1F5F9]">Invitations par QR code</h1>
                <p className="mt-1 text-sm text-[#94A3B8]">
                    Générez un QR code (ou lien) sécurisé pour qu’un collaborateur rejoigne votre entreprise.
                    Validité limitée dans le temps, nombre d’usages borné, révocable à tout moment.
                </p>
            </div>
            <InvitesManager />
        </div>
    );
}
