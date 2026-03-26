import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { AuthResponse, Role } from "../../types/hrms";

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  expiresUtc: string | null;
  userId: string | null;
  employeeId: string | null;
  email: string | null;
  roles: Role[];
  setSession: (payload: AuthResponse) => void;
  clearSession: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      expiresUtc: null,
      userId: null,
      employeeId: null,
      email: null,
      roles: [],
      setSession: (payload) =>
        set({
          accessToken: payload.accessToken,
          refreshToken: payload.refreshToken,
          expiresUtc: payload.expiresUtc,
          userId: payload.userId,
          employeeId: payload.employeeId ?? null,
          email: payload.email,
          roles: payload.roles,
        }),
      clearSession: () =>
        set({
          accessToken: null,
          refreshToken: null,
          expiresUtc: null,
          userId: null,
          employeeId: null,
          email: null,
          roles: [],
        }),
    }),
    {
      name: "hrms-auth",
    },
  ),
);
