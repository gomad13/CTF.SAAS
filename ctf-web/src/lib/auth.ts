function base64UrlDecode(input: string) {
    const base64 = input.replace(/-/g, "+").replace(/_/g, "/");
    const padded = base64 + "===".slice((base64.length + 3) % 4);
    const decoded = atob(padded);
    try {
        return decodeURIComponent(
            decoded
                .split("")
                .map((c) => "%" + c.charCodeAt(0).toString(16).padStart(2, "0"))
                .join("")
        );
    } catch {
        return decoded;
    }
}

export function getToken(): string | null {
    if (typeof window === "undefined") return null;
    return localStorage.getItem("token");
}

export function getJwtPayload(): any | null {
    const token = getToken();
    if (!token) return null;

    const parts = token.split(".");
    if (parts.length !== 3) return null;

    try {
        const json = base64UrlDecode(parts[1]);
        return JSON.parse(json);
    } catch {
        return null;
    }
}

export function getRole(): string | null {
    const payload = getJwtPayload();
    return payload?.role ?? payload?.Role ?? null;
}
