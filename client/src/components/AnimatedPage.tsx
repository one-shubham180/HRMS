import type { PropsWithChildren } from "react";

export function AnimatedPage({ children }: PropsWithChildren) {
  return <section className="page-enter space-y-6">{children}</section>;
}
