import type { SessionUser } from "~/types/admin";

/**
 * Client-side dedupe for concurrent `/v1/auth/me` calls (the navbar and a route
 * middleware both want the session on the same navigation). Deliberately not
 * module state on the server, where it would leak across requests.
 */
let inFlight: Promise<SessionUser | null> | null = null;

function statusOf(error: unknown) {
  const candidate = error as {
    statusCode?: number;
    status?: number;
    response?: { status?: number };
  };
  return (
    candidate?.statusCode ?? candidate?.status ?? candidate?.response?.status
  );
}

export function useAuthSession() {
  const { $api } = useNuxtApp();
  const user = useState<SessionUser | null>("zmovie:session-user", () => null);
  const isResolved = useState<boolean>("zmovie:session-resolved", () => false);

  async function load(): Promise<SessionUser | null> {
    try {
      user.value = await $api<SessionUser>("/v1/auth/me", {
        credentials: "include",
      });
      isResolved.value = true;
    } catch (error: unknown) {
      const status = statusOf(error);
      // 401/403 is a definite answer: this visitor is signed out, cache it.
      // Anything else (network blip, 5xx, CORS) is not an answer — leave the
      // session unresolved so the next caller retries instead of the app
      // treating a transient failure as a logout for the rest of the session.
      user.value = null;
      isResolved.value = status === 401 || status === 403;
    }
    return user.value;
  }

  async function fetchSession(force = false): Promise<SessionUser | null> {
    if (isResolved.value && !force) return user.value;
    if (!import.meta.client) return load();
    if (!inFlight || force) inFlight = load().finally(() => (inFlight = null));
    return inFlight;
  }

  async function signOut() {
    await $api("/v1/auth/logout", {
      method: "POST",
      credentials: "include",
    }).catch(() => undefined);
    user.value = null;
    isResolved.value = true;
  }

  return {
    user,
    isResolved,
    isAdmin: computed(() => user.value?.role === "admin"),
    fetchSession,
    signOut,
  };
}
