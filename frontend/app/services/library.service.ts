import type { ApiFetch } from "~/types/api-fetch";
import type { Library } from "~/types/library";
import type { CreateReviewPayload } from "~/types/review";

function resolveApi(api?: ApiFetch): ApiFetch {
  return api ?? (useNuxtApp().$api as ApiFetch);
}

export function fetchUserLibrary(
  locale?: string,
  api?: ApiFetch,
): Promise<Library> {
  return resolveApi(api)<Library>("/v1/me/library", {
    credentials: "include",
    query: locale ? { locale } : undefined,
  });
}

export function saveTitleToLibrary(
  slug: string,
  api?: ApiFetch,
): Promise<void> {
  return resolveApi(api)(`/v1/me/saved/${encodeURIComponent(slug)}`, {
    method: "PUT",
    credentials: "include",
  }).then(() => undefined);
}

export function removeTitleFromLibrary(
  slug: string,
  api?: ApiFetch,
): Promise<void> {
  return resolveApi(api)(`/v1/me/saved/${encodeURIComponent(slug)}`, {
    method: "DELETE",
    credentials: "include",
  }).then(() => undefined);
}

export function recordWatchHistory(
  slug: string,
  data: { episodeNumber?: number | null; progressSeconds: number },
  keepalive = false,
  api?: ApiFetch,
): Promise<void> {
  return resolveApi(api)(`/v1/me/history/${encodeURIComponent(slug)}`, {
    method: "POST",
    credentials: "include",
    keepalive,
    body: {
      episodeNumber: data.episodeNumber ?? null,
      progressSeconds: data.progressSeconds,
    },
  }).then(() => undefined);
}

export function submitTitleReview(
  slug: string,
  payload: CreateReviewPayload,
  api?: ApiFetch,
): Promise<void> {
  return resolveApi(api)(`/v1/me/titles/${encodeURIComponent(slug)}/review`, {
    method: "PUT",
    credentials: "include",
    body: payload,
  }).then(() => undefined);
}
