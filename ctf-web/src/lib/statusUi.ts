export function statusLabel(color: string) {
    if (color === "grey") return "Pas commenc�";
    if (color === "yellow") return "En cours";
    if (color === "green") return "R�ussi";
    return "�chec";
}

export function statusBadgeClass(color: string) {
    if (color === "grey") return "bg-neutral-800 text-black border-neutral-700";
    if (color === "yellow") return "bg-yellow-950 text-yellow-200 border-yellow-800";
    if (color === "green") return "bg-primary/20 text-primary border-primary";
    return "bg-red-950 text-red-200 border-red-800";
}
