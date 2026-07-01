// QR 3 types — Types des invitations QR (App / Entreprise-Inscription / Entreprise-Rejoindre).

/** Type d'invitation QR. */
export type InviteType = "app" | "enterprise_signup" | "enterprise_join";

/** Invitation listée côté admin (jamais le token). */
export interface InviteDto {
    id: string;
    expiresAt: string;
    maxUses: number;
    usedCount: number;
    isRevoked: boolean;
    isExpired: boolean;
    createdAt: string;
    type: InviteType;
    tenantId: string | null;
    tenantName: string | null;
}

/** Réponse à la création — contient le token en clair UNE seule fois. */
export interface CreatedInviteDto {
    id: string;
    token: string;
    joinUrl: string;
    expiresAt: string;
    maxUses: number;
    type: InviteType;
}

/** Corps de la requête de création. */
export interface CreateInviteRequest {
    expiresInHours: number;
    maxUses: number;
    type: InviteType;
}

/** Résultat d'un redeem réussi. */
export interface RedeemResultDto {
    tenantId: string;
    tenantName: string;
}
