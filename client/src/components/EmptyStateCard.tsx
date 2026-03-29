import type { ReactNode } from "react";
import { FileSearch } from "lucide-react";

interface EmptyStateCardProps {
  title: string;
  description: string;
  icon?: ReactNode;
}

export function EmptyStateCard({ title, description, icon }: EmptyStateCardProps) {
  return (
    <div className="relative overflow-hidden rounded-3xl border border-dashed border-slate-200 bg-gradient-to-br from-slate-50 via-white to-sand/45 px-5 py-8 text-center">
      <div className="absolute right-4 top-4 h-16 w-16 rounded-full bg-ember/8 blur-2xl" />
      <div className="absolute bottom-2 left-6 h-16 w-16 rounded-full bg-lagoon/8 blur-2xl" />
      <div className="relative mx-auto flex max-w-md flex-col items-center">
        <div className="rounded-[1.75rem] bg-white p-4 shadow-panel">
          {icon ?? <FileSearch className="h-8 w-8 text-lagoon" strokeWidth={1.8} />}
        </div>
        <h3 className="mt-4 font-display text-2xl text-ink">{title}</h3>
        <p className="mt-2 text-sm leading-6 text-slate-600">{description}</p>
      </div>
    </div>
  );
}
