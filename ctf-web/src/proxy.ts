import { NextRequest, NextResponse } from "next/server";

const PUBLIC_PATHS = [
    "/",
    "/login",
    "/register",
    "/landing",
    "/forgot-password",
    "/reset-password",
    "/demo",
    "/cgu",
    "/privacy",
    "/mentions-legales",
    "/feedback",
    "/join",
];
const ADMIN_PATHS = ["/admin"];
const SUPERADMIN_PATHS = ["/superadmin"];

export function proxy(request: NextRequest) {
    const { pathname, searchParams } = request.nextUrl;

    // [FIX QR — invitation entreprise] Un utilisateur NON connecté qui ouvre le lien
    // d'invitation /join?token=... doit arriver sur l'INSCRIPTION (société pré-remplie),
    // PAS sur /login. Redirection côté serveur (307) qui CONSERVE le token :
    //   - non connecté + token  -> /register?token=<token>  (nouveau compte, Type 2)
    //   - connecté (cookie jwt)  -> on laisse passer, la page /join fait le rattachement (Type 3)
    // Le token est base64url (aucun '/'), aucun double-encodage possible.
    if (pathname === "/join") {
        const token = searchParams.get("token");
        const jwt = request.cookies.get("jwt")?.value;
        if (token && !jwt) {
            const reg = new URL("/register", request.url);
            reg.searchParams.set("token", token);
            return NextResponse.redirect(reg);
        }
    }

    // Laisser passer les assets, API publiques et routes publiques
    if (
        pathname.startsWith("/_next") ||
        pathname.startsWith("/favicon") ||
        pathname.startsWith("/images") ||
        pathname.includes(".") ||
        PUBLIC_PATHS.some(p => pathname === p || pathname.startsWith(p + "/"))
    ) {
        return NextResponse.next();
    }

    const jwtCookie = request.cookies.get("jwt")?.value;
    const roleCookie = request.cookies.get("user_role")?.value;

    // Pas de JWT → /login
    if (!jwtCookie) {
        const url = new URL("/login", request.url);
        // searchParams.set encode déjà la valeur une fois. Ne PAS pré-encoder
        // (sinon double encodage : "/dashboard" -> %2F -> %252F).
        url.searchParams.set("next", pathname);
        return NextResponse.redirect(url);
    }

    // SuperAdmin — bloque seulement si role explicitement DIFFÉRENT
    // Si roleCookie absent → laisser passer (la page vérifie via /api/auth/me)
    if (SUPERADMIN_PATHS.some(p => pathname.startsWith(p))) {
        if (roleCookie && roleCookie !== "SuperAdmin") {
            return NextResponse.redirect(new URL("/dashboard", request.url));
        }
    }

    // Admin — idem
    if (ADMIN_PATHS.some(p => pathname.startsWith(p))) {
        if (roleCookie && roleCookie !== "admin" && roleCookie !== "Admin" && roleCookie !== "SuperAdmin") {
            return NextResponse.redirect(new URL("/dashboard", request.url));
        }
    }

    return NextResponse.next();
}

export const config = {
    matcher: ["/((?!_next/static|_next/image|favicon.ico|api).*)"],
};
