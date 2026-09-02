import type { ApiFetch } from "~/types/api-fetch";
import type { TitleListResponse } from "~/types/catalog";

function resolveApi(api?: ApiFetch): ApiFetch {
  return api ?? (useNuxtApp().$api as ApiFetch);
}

export function searchCatalogTitles(
  params: { q: string; locale?: string; type?: string; genre?: string },
  api?: ApiFetch,
): Promise<TitleListResponse> {
  return resolveApi(api)<TitleListResponse>("/v1/search", {
    query: params,
  });
}
