export function statusLabel(color: string) {
    if (color === "grey") return "Pas commencé";
    if (color === "yellow") return "En cours";
    if (color === "green") return "Réussi";
    return "Échec";
}

export function statusBadgeClass(color: string) {
    if (color === "grey") return "bg-neutral-800 text-neutral-200 border-neutral-700";
    if (color === "yellow") return "bg-yellow-950 text-yellow-200 border-yellow-800";
    if (color === "green") return "bg-green-950 text-green-200 border-green-800";
    return "bg-red-950 text-red-200 border-red-800";
}
