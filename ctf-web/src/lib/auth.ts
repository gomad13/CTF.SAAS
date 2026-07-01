// Le token JWT est stocké dans un cookie HttpOnly : inaccessible depuis JavaScript.
// Pour lire le rôle ou les claims, appelez une route API authentifiée
// (ex. GET /api/auth/me) et récupérez les infos depuis la réponse JSON.

export function getJwtPayload(): Record<string, unknown> | null {
    return null;
}

export function getRole(): string | null {
    return null;
}