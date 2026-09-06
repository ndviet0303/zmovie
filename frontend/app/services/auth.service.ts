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

export function passwordAuth(
  login: string,
  password: string,
  api?: ApiFetch,
): Promise<SessionUser> {
  return resolveApi(api)<SessionUser>("/v1/auth/login", {
    method: "POST",
    credentials: "include",
    body: { login, password },
  });
}

export function registerAuth(
  payload: {
    username: string;
    displayName: string;
    email: string;
    password: string;
  },
  api?: ApiFetch,
): Promise<SessionUser> {
  return resolveApi(api)<SessionUser>("/v1/auth/register", {
    method: "POST",
    credentials: "include",
    body: payload,
  });
}

export function requestPasswordReset(
  email: string,
  api?: ApiFetch,
): Promise<void> {
  return resolveApi(api)("/v1/auth/forgot-password", {
    method: "POST",
    body: { email },
  }).then(() => undefined);
}

export function resetPassword(
  login: string,
  token: string,
  password: string,
  api?: ApiFetch,
): Promise<void> {
  return resolveApi(api)("/v1/auth/reset-password", {
    method: "POST",
    body: { login, token, password },
  }).then(() => undefined);
}
