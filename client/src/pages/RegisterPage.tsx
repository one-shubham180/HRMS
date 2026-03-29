import { Link } from "react-router-dom";

export function RegisterPage() {
  return (
    <div className="min-h-screen px-4 py-8 lg:px-8">
      <div className="mx-auto grid min-h-[calc(100vh-4rem)] max-w-5xl items-center gap-8 lg:grid-cols-[0.95fr_1.05fr]">
        <section className="page-enter panel p-8 lg:p-10">
          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-lagoon">Admin-Driven Onboarding</p>
          <h1 className="mt-4 font-display text-4xl text-ink">Public registration is disabled</h1>
          <p className="mt-4 text-sm leading-7 text-slate-600">
            Employee accounts are now created by HR or Admin users only. New joiners receive a welcome email and password setup link after their profile is provisioned.
          </p>
          <div className="mt-8 rounded-3xl bg-sand p-5">
            <p className="text-sm font-semibold text-ink">What to do instead</p>
            <ul className="mt-3 space-y-2 text-sm text-slate-600">
              <li>Ask HR or an administrator to create your employee profile.</li>
              <li>Use the password setup email sent during onboarding.</li>
              <li>Return to sign in after your account is activated.</li>
            </ul>
          </div>
        </section>

        <section className="panel page-enter space-y-5 p-8 lg:p-10">
          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-ember">Why this changed</p>
          <h2 className="font-display text-3xl text-ink">Controlled onboarding for enterprise HRMS</h2>
          <p className="text-sm leading-7 text-slate-600">
            This workspace now follows an admin-controlled lifecycle so access, auditability, and deactivation rules stay consistent across payroll, attendance, and employee records.
          </p>
          <div className="rounded-3xl border border-slate-100 bg-slate-50 p-5">
            <p className="text-sm font-semibold text-ink">For HR and admins</p>
            <p className="mt-2 text-sm text-slate-600">
              Create users from the employee management flow, then guide them to sign in after the welcome email is issued.
            </p>
          </div>
          <div className="flex flex-col gap-3 sm:flex-row">
            <Link className="btn-primary" to="/login">
              Back to sign in
            </Link>
          </div>
        </section>
      </div>
    </div>
  );
}
