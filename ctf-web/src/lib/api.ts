const BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL;

if (!BASE_URL) {
    throw new Error("NEXT_PUBLIC_API_BASE_URL is missing in .env.local");
}

type FetchOpts = Omit<RequestInit, "credentials">;

async function doFetch(path: string, opts: FetchOpts) {
    const headers = new Headers(opts.headers);
    if (!headers.has("Content-Type") && opts.body && !(opts.body instanceof FormData)) {
        headers.set("Content-Type", "application/json");
    }
    // CSRF protection — header requis par le backend sur POST/PUT/DELETE/PATCH
    headers.set("X-Requested-With", "XMLHttpRequest");
    return fetch(`${BASE_URL}${path}`, { ...opts, headers, credentials: "include" });
}

export async function apiFetch<T>(path: string, opts: FetchOpts = {}): Promise<T> {
    let res = await doFetch(path, opts);

    // 401 → tenter un refresh automatique (sauf sur les routes auth)
    if (res.status === 401 && !path.startsWith("/api/auth")) {
        const refreshRes = await fetch(`${BASE_URL}/api/auth/refresh`, {
            method: "POST",
            credentials: "include",
            headers: { "X-Requested-With": "XMLHttpRequest" },
        });
        if (refreshRes.ok) {
            res = await doFetch(path, opts);
        } else {
            window.location.replace("/login");
            throw new Error("Session expirée.");
        }
    }

    if (res.status === 401 && !path.startsWith("/api/auth")) {
        window.location.replace("/login");
        throw new Error("Session expirée.");
    }

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
