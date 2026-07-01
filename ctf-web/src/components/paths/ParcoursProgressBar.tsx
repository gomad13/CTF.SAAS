"use client";

type Props = {
    completed: number;
    total: number;
    label?: string;
};

export default function ParcoursProgressBar({ completed, total, label }: Props) {
    const percent = total > 0 ? Math.round((completed / total) * 100) : 0;

    return (
        <div className="flex flex-col gap-2">
            <div className="flex items-center justify-between text-xs font-medium text-fg-muted">
                <span>{label ?? `${completed}/${total} challenges complétés`}</span>
                <span className="font-semibold text-fg-heading">{percent}%</span>
            </div>
            <div className="h-1.5 w-full overflow-hidden rounded-full bg-border">
                <div
                    className="h-full rounded-full bg-primary transition-[width] duration-500"
                    style={{ width: `${percent}%` }}
                />
            </div>
        </div>
    );
}
