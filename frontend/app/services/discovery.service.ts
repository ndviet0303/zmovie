import type { ApiFetch } from "~/types/api-fetch";
import type {
  HomeResponse,
  PersonalizedDiscovery,
  TopPeriod,
  TopTitle,
} from "~/types/discovery";

function resolveApi(api?: ApiFetch): ApiFetch {
  return api ?? (useNuxtApp().$api as ApiFetch);
}

export function fetchHomeDiscovery(
  locale?: string,
  api?: ApiFetch,
): Promise<HomeResponse> {
  return resolveApi(api)<HomeResponse>("/v1/discovery/home", {
    query: locale ? { locale } : undefined,
  });
}

export function fetchTopTitles(
  period: TopPeriod,
  params?: { locale?: string; limit?: number },
  api?: ApiFetch,
): Promise<TopTitle[]> {
  return resolveApi(api)<TopTitle[]>(`/v1/discovery/top/${period}`, {
    query: params,
  });
}

export function fetchPersonalizedDiscovery(
  locale?: string,
  api?: ApiFetch,
): Promise<PersonalizedDiscovery> {
  return resolveApi(api)<PersonalizedDiscovery>("/v1/discovery/for-you", {
    credentials: "include",
    query: locale ? { locale } : undefined,
  });
}
