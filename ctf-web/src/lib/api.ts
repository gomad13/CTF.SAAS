const BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL;

if (!BASE_URL) {
    throw new Error("NEXT_PUBLIC_API_BASE_URL is missing in .env.local");
}

export function getToken() {
    if (typeof window === "undefined") return null;
    return localStorage.getItem("ctf_token");
}

export function setToken(token: string) {
    localStorage.setItem("ctf_token", token);
}

export function clearToken() {
    localStorage.removeItem("ctf_token");
}

type FetchOpts = RequestInit & { auth?: boolean };

export async function apiFetch<T>(path: string, opts: FetchOpts = {}): Promise<T> {
    const headers = new Headers(opts.headers);

    if (!headers.has("Content-Type") && opts.body) {
        headers.set("Content-Type", "application/json");
    }

    if (opts.auth !== false) {
        const token = getToken();
        if (token) headers.set("Authorization", `Bearer ${token}`);
    }

    const res = await fetch(`${BASE_URL}${path}`, { ...opts, headers });

    if (res.status === 204) return undefined as T;

    const text = await res.text();
    const data = text ? safeJson(text) : null;

    if (!res.ok) {
        const message = (data && (data.error || data.message)) || `HTTP ${res.status}`;
        throw new Error(message);
    }

    return data as T;
}

function safeJson(text: string) {
    try {
        return JSON.parse(text);
    } catch {
        return { raw: text };
    }
}
