import axios from "axios";
import { useAuthStore } from "../features/auth/authStore";
import type { AuthResponse } from "../types/hrms";

const baseURL = import.meta.env.VITE_API_URL ?? "http://localhost:5108/api";

export const apiClient = axios.create({
  baseURL,
});

apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

let refreshInFlight: Promise<string | null> | null = null;

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as typeof error.config & { _retry?: boolean };

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      if (!refreshInFlight) {
        refreshInFlight = refreshSession();
      }

      const token = await refreshInFlight;
      refreshInFlight = null;

      if (token) {
        originalRequest.headers = {
          ...(originalRequest.headers ?? {}),
          Authorization: `Bearer ${token}`,
        };
        return apiClient(originalRequest);
      }
    }

    return Promise.reject(error);
  },
);

async function refreshSession(): Promise<string | null> {
  const { refreshToken, setSession, clearSession } = useAuthStore.getState();

  if (!refreshToken) {
    clearSession();
    return null;
  }

  try {
    const response = await axios.post<AuthResponse>(`${baseURL}/auth/refresh-token`, {
      refreshToken,
    });
    setSession(response.data);
    return response.data.accessToken;
  } catch {
    clearSession();
    window.location.href = "/login";
    return null;
  }
}
