import { Link } from "react-router-dom";

export function NotFoundPage() {
  return (
    <div className="flex min-h-screen items-center justify-center px-4">
      <div className="panel page-enter max-w-xl p-10 text-center">
        <p className="text-xs font-semibold uppercase tracking-[0.35em] text-lagoon">404</p>
        <h1 className="mt-4 font-display text-4xl text-ink">That page drifted off the org chart.</h1>
        <p className="mt-4 text-sm text-slate-600">The route does not exist or your session redirected you somewhere stale.</p>
        <Link className="btn-primary mt-8" to="/dashboard">
          Return to dashboard
        </Link>
      </div>
    </div>
  );
}
