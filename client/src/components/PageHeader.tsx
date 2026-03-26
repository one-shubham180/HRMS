import type { PropsWithChildren, ReactNode } from "react";

interface PageHeaderProps extends PropsWithChildren {
  title: string;
  subtitle: string;
  actions?: ReactNode;
}

export function PageHeader({ title, subtitle, actions, children }: PageHeaderProps) {
  return (
    <div className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
      <div className="space-y-2">
        <p className="text-xs font-semibold uppercase tracking-[0.35em] text-lagoon">HRMS Workspace</p>
        <h1 className="font-display text-3xl text-ink">{title}</h1>
        <p className="max-w-2xl text-sm text-slate-600">{subtitle}</p>
        {children}
      </div>
      {actions ? <div className="flex flex-wrap gap-3">{actions}</div> : null}
    </div>
  );
}
