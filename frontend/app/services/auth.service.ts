import type { ApiFetch } from "~/types/api-fetch";
import type { SessionUser } from "~/types/auth";

function resolveApi(api?: ApiFetch): ApiFetch {
  return api ?? (useNuxtApp().$api as ApiFetch);
}

export function fetchAuthMe(api?: ApiFetch): Promise<SessionUser> {
  return resolveApi(api)<SessionUser>("/v1/auth/me", {
    credentials: "include",
  });
}

export function logoutAuth(api?: ApiFetch): Promise<void> {
  return resolveApi(api)("/v1/auth/logout", {
    method: "POST",
    credentials: "include",
  }).then(() => undefined);
}

export function googleAuth(
  credential: string,
  api?: ApiFetch,
): Promise<SessionUser> {
  return resolveApi(api)<SessionUser>("/v1/auth/google", {
    method: "POST",
    credentials: "include",
    body: { credential },
  });
}
