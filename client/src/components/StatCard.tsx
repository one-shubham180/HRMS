interface StatCardProps {
  label: string;
  value: string;
  hint: string;
}

export function StatCard({ label, value, hint }: StatCardProps) {
  return (
    <div className="panel p-6">
      <p className="text-xs font-semibold uppercase tracking-[0.25em] text-slate-500">{label}</p>
      <p className="mt-4 font-display text-4xl text-ink">{value}</p>
      <p className="mt-3 text-sm text-slate-600">{hint}</p>
    </div>
  );
}
