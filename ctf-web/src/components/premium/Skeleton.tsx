export function Skeleton({ className = "", style }: { className?: string; style?: React.CSSProperties }) {
    return <div className={`skel ${className}`} style={{ height: 16, ...style }} aria-hidden />;
}
export function SkeletonCard() {
    return (
        <div className="rounded-xl border border-border bg-surface p-6">
            <Skeleton style={{ width: "40%", height: 12 }} />
            <Skeleton style={{ width: "60%", height: 28, marginTop: 12 }} />
        </div>
    );
}
export function SkeletonRows({ rows = 5 }: { rows?: number }) {
    return <div className="flex flex-col gap-2">{Array.from({ length: rows }).map((_, i) => <Skeleton key={i} style={{ height: 40 }} />)}</div>;
}
