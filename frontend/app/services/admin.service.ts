import type {
  AdminGenreSummary,
  AdminOverview,
  AdminReviewSummary,
  AdminTitleDetail,
  AdminTitleEdit,
  AdminTitleSummary,
  AdminUserSummary,
  Paged,
  UserRole,
} from "~/types/admin";
import type { ApiFetch } from "~/types/api-fetch";

function resolveApi(api?: ApiFetch): ApiFetch {
  return api ?? (useNuxtApp().$api as ApiFetch);
}

// Overview
export function fetchAdminOverview(api?: ApiFetch): Promise<AdminOverview> {
  return resolveApi(api)<AdminOverview>("/v1/admin/overview", {
    credentials: "include",
  });
}

// Titles
export function fetchAdminTitles(
  params?: {
    q?: string;
    genre?: string;
    type?: string;
    featured?: string;
    page?: number;
    pageSize?: number;
  },
  api?: ApiFetch,
): Promise<Paged<AdminTitleSummary>> {
  return resolveApi(api)<Paged<AdminTitleSummary>>("/v1/admin/titles", {
    credentials: "include",
    query: params,
  });
}

export function fetchAdminTitle(
  slug: string,
  api?: ApiFetch,
): Promise<AdminTitleDetail> {
  return resolveApi(api)<AdminTitleDetail>(
    `/v1/admin/titles/${encodeURIComponent(slug)}`,
    { credentials: "include" },
  );
}

export function updateAdminTitle(
  slug: string,
  form: AdminTitleEdit,
  api?: ApiFetch,
): Promise<AdminTitleDetail> {
  return resolveApi(api)<AdminTitleDetail>(
    `/v1/admin/titles/${encodeURIComponent(slug)}`,
    {
      method: "PUT",
      credentials: "include",
      body: form,
    },
  );
}

export function deleteAdminTitle(slug: string, api?: ApiFetch): Promise<void> {
  return resolveApi(api)(`/v1/admin/titles/${encodeURIComponent(slug)}`, {
    method: "DELETE",
    credentials: "include",
  }).then(() => undefined);
}

export function toggleAdminTitleFeatured(
  slug: string,
  featured: boolean,
  api?: ApiFetch,
): Promise<AdminTitleDetail> {
  return resolveApi(api)<AdminTitleDetail>(
    `/v1/admin/titles/${encodeURIComponent(slug)}/featured`,
    {
      method: "PATCH",
      credentials: "include",
      body: { featured },
    },
  );
}

// Genres
export function fetchAdminGenres(api?: ApiFetch): Promise<AdminGenreSummary[]> {
  return resolveApi(api)<AdminGenreSummary[]>("/v1/admin/genres", {
    credentials: "include",
  });
}

export function createAdminGenre(
  data: { name: string; slug: string },
  api?: ApiFetch,
): Promise<AdminGenreSummary> {
  return resolveApi(api)<AdminGenreSummary>("/v1/admin/genres", {
    method: "POST",
    credentials: "include",
    body: data,
  });
}

export function updateAdminGenre(
  id: string,
  data: { name: string; slug?: string },
  api?: ApiFetch,
): Promise<AdminGenreSummary> {
  return resolveApi(api)<AdminGenreSummary>(`/v1/admin/genres/${id}`, {
    method: "PUT",
    credentials: "include",
    body: data,
  });
}

export function deleteAdminGenre(id: string, api?: ApiFetch): Promise<void> {
  return resolveApi(api)(`/v1/admin/genres/${id}`, {
    method: "DELETE",
    credentials: "include",
  }).then(() => undefined);
}

// Users
export function fetchAdminUsers(
  params?: { q?: string; role?: string; page?: number; pageSize?: number },
  api?: ApiFetch,
): Promise<Paged<AdminUserSummary>> {
  return resolveApi(api)<Paged<AdminUserSummary>>("/v1/admin/users", {
    credentials: "include",
    query: params,
  });
}

export function updateAdminUserRole(
  id: string,
  role: UserRole,
  api?: ApiFetch,
): Promise<AdminUserSummary> {
  return resolveApi(api)<AdminUserSummary>(`/v1/admin/users/${id}/role`, {
    method: "PATCH",
    credentials: "include",
    body: { role },
  });
}

// Reviews
export function fetchAdminReviews(
  params?: { q?: string; maxRating?: string; page?: number; pageSize?: number },
  api?: ApiFetch,
): Promise<Paged<AdminReviewSummary>> {
  return resolveApi(api)<Paged<AdminReviewSummary>>("/v1/admin/reviews", {
    credentials: "include",
    query: params,
  });
}

export function deleteAdminReview(id: string, api?: ApiFetch): Promise<void> {
  return resolveApi(api)(`/v1/admin/reviews/${id}`, {
    method: "DELETE",
    credentials: "include",
  }).then(() => undefined);
}
