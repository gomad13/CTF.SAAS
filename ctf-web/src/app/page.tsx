import { redirect } from "next/navigation";

// Redirige immédiatement vers la landing page publique.
// La landing redirige ensuite vers /login ou /dashboard selon l'action de l'utilisateur.
export default function Home() {
    redirect("/landing");
}