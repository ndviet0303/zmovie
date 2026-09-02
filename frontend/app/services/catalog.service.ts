import type { ApiFetch } from "~/types/api-fetch";
import type {
  PlaybackResponse,
  TitleDetail,
  TitleListResponse,
} from "~/types/catalog";
import type { ReviewsResponse } from "~/types/review";
import type { ViewRecordedResponse } from "~/types/watch";

function resolveApi(api?: ApiFetch): ApiFetch {
  return api ?? (useNuxtApp().$api as ApiFetch);
}

export function fetchCatalogTitles(
  params?: { q?: string; genre?: string; locale?: string },
  api?: ApiFetch,
): Promise<TitleListResponse> {
  return resolveApi(api)<TitleListResponse>("/v1/catalog/titles", {
    query: params,
  });
}

export function fetchCatalogTitleBySlug(
  slug: string,
  locale?: string,
  api?: ApiFetch,
): Promise<TitleDetail> {
  return resolveApi(api)<TitleDetail>(
    `/v1/catalog/titles/${encodeURIComponent(slug)}`,
    {
      query: locale ? { locale } : undefined,
    },
  );
}

export function fetchCatalogGenres(api?: ApiFetch): Promise<string[]> {
  return resolveApi(api)<string[]>("/v1/catalog/genres");
}

export function fetchCatalogPlayback(
  slug: string,
  locale?: string,
  api?: ApiFetch,
): Promise<PlaybackResponse> {
  return resolveApi(api)<PlaybackResponse>(
    `/v1/catalog/titles/${encodeURIComponent(slug)}/playback`,
    {
      query: locale ? { locale } : undefined,
    },
  );
}

export function fetchTitleReviews(
  slug: string,
  api?: ApiFetch,
): Promise<ReviewsResponse> {
  return resolveApi(api)<ReviewsResponse>(
    `/v1/catalog/titles/${encodeURIComponent(slug)}/reviews`,
  );
}

export function recordTitleView(
  slug: string,
  episodeNumber?: number | null,
  api?: ApiFetch,
): Promise<ViewRecordedResponse> {
  return resolveApi(api)<ViewRecordedResponse>(
    `/v1/catalog/titles/${encodeURIComponent(slug)}/views`,
    {
      method: "POST",
      credentials: "include",
      body: {
        episodeNumber: episodeNumber ?? null,
      },
    },
  );
}

export function reportTitleIssue(
  slug: string,
  payload: { category: string; description: string; timestampSeconds?: number },
  api?: ApiFetch,
): Promise<void> {
  return resolveApi(api)(
    `/v1/catalog/titles/${encodeURIComponent(slug)}/reports`,
    {
      method: "POST",
      body: payload,
    },
  ).then(() => undefined);
}
