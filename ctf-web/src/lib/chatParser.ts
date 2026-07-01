export interface ParsedSegment {
    type: "text" | "conseil" | "danger";
    content: string;
}

export function parseAriaMessage(raw: string): ParsedSegment[] {
    const segments: ParsedSegment[] = [];

    // Découper en blocs séparés par lignes vides
    const blocks = raw.split(/\n\s*\n/).map(b => b.trim()).filter(b => b.length > 0);

    for (const block of blocks) {
        const lines = block.split("\n").map(l => l.trim()).filter(l => l.length > 0);

        // Texte courant à fusionner pour ce bloc
        let textBuffer = "";

        const flushText = () => {
            if (textBuffer) {
                segments.push({ type: "text", content: textBuffer.trim() });
                textBuffer = "";
            }
        };

        for (const line of lines) {
            if (line.startsWith("[CONSEIL]")) {
                flushText();
                segments.push({ type: "conseil", content: line.replace("[CONSEIL]", "").trim() });
            } else if (line.startsWith("[DANGER]")) {
                flushText();
                segments.push({ type: "danger", content: line.replace("[DANGER]", "").trim() });
            } else {
                textBuffer += (textBuffer ? " " : "") + line;
            }
        }

        flushText();
    }

    return segments.filter(s => s.content.length > 0);
}
