// V4 — Types des invitations QR pour rejoindre une entreprise (tenant).

/** Invitation listée côté admin (jamais le token). */
export interface InviteDto {
    id: string;
    expiresAt: string;
    maxUses: number;
    usedCount: number;
    isRevoked: boolean;
    isExpired: boolean;
    createdAt: string;
}

/** Réponse à la création — contient le token en clair UNE seule fois. */
export interface CreatedInviteDto {
    id: string;
    token: string;
    joinUrl: string;
    expiresAt: string;
    maxUses: number;
}

/** Corps de la requête de création. */
export interface CreateInviteRequest {
    expiresInHours: number;
    maxUses: number;
}

/** Résultat d'un redeem réussi. */
export interface RedeemResultDto {
    tenantId: string;
    tenantName: string;
}
