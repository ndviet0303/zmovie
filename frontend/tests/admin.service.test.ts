import { describe, expect, it } from "bun:test";
import {
  deleteAdminGenre,
  deleteAdminReview,
  deleteAdminTitle,
  fetchAdminGenres,
  fetchAdminOverview,
  fetchAdminReviews,
  fetchAdminTitles,
  fetchAdminUsers,
  toggleAdminTitleFeatured,
  updateAdminUserRole,
} from "../app/services/admin.service";
import { createMockApi } from "./test-utils";

describe("admin.service", () => {
  it("fetchAdminOverview queries /v1/admin/overview with credentials", async () => {
    const { api, lastCall } = createMockApi(() =>
      Promise.resolve({
        totalTitles: 10,
        totalUsers: 20,
        totalReviews: 30,
        totalViews: 400,
        recentTitles: [],
        recentUsers: [],
      }),
    );

    const overview = await fetchAdminOverview(api);
    expect(lastCall().url).toBe("/v1/admin/overview");
    expect(lastCall().options?.credentials).toBe("include");
    expect(overview.totalTitles).toBe(10);
  });

  it("fetchAdminTitles queries /v1/admin/titles with params", async () => {
    const { api, lastCall } = createMockApi(() =>
      Promise.resolve({ items: [], total: 0, page: 1, pageSize: 20 }),
    );

    await fetchAdminTitles({ q: "matrix", page: 2 }, api);
    expect(lastCall().url).toBe("/v1/admin/titles");
    expect(lastCall().options?.query).toEqual({ q: "matrix", page: 2 });
  });

  it("toggleAdminTitleFeatured patches /v1/admin/titles/:slug/featured", async () => {
    const { api, lastCall } = createMockApi(() =>
      Promise.resolve({ slug: "matrix", isFeatured: true }),
    );

    await toggleAdminTitleFeatured("matrix", true, api);
    expect(lastCall().url).toBe("/v1/admin/titles/matrix/featured");
    expect(lastCall().options?.method).toBe("PATCH");
    expect(lastCall().options?.body).toEqual({ featured: true });
  });

  it("deleteAdminTitle deletes title", async () => {
    const { api, lastCall } = createMockApi(() => Promise.resolve());

    await deleteAdminTitle("matrix", api);
    expect(lastCall().url).toBe("/v1/admin/titles/matrix");
    expect(lastCall().options?.method).toBe("DELETE");
  });

  it("deleteAdminGenre deletes genre", async () => {
    const { api, lastCall } = createMockApi(() => Promise.resolve());

    await deleteAdminGenre("genre-1", api);
    expect(lastCall().url).toBe("/v1/admin/genres/genre-1");
    expect(lastCall().options?.method).toBe("DELETE");
  });

  it("updateAdminUserRole patches user role", async () => {
    const { api, lastCall } = createMockApi(() =>
      Promise.resolve({ id: "u-1", role: "admin" }),
    );

    await updateAdminUserRole("u-1", "admin", api);
    expect(lastCall().url).toBe("/v1/admin/users/u-1/role");
    expect(lastCall().options?.method).toBe("PATCH");
    expect(lastCall().options?.body).toEqual({ role: "admin" });
  });

  it("deleteAdminReview deletes review", async () => {
    const { api, lastCall } = createMockApi(() => Promise.resolve());

    await deleteAdminReview("rev-1", api);
    expect(lastCall().url).toBe("/v1/admin/reviews/rev-1");
    expect(lastCall().options?.method).toBe("DELETE");
  });

  it("fetchAdminGenres queries /v1/admin/genres", async () => {
    const { api, lastCall } = createMockApi(() => Promise.resolve([]));

    await fetchAdminGenres(api);
    expect(lastCall().url).toBe("/v1/admin/genres");
  });

  it("fetchAdminReviews queries /v1/admin/reviews", async () => {
    const { api, lastCall } = createMockApi(() =>
      Promise.resolve({ items: [], total: 0, page: 1, pageSize: 20 }),
    );

    await fetchAdminReviews({ maxRating: "3" }, api);
    expect(lastCall().url).toBe("/v1/admin/reviews");
    expect(lastCall().options?.query).toEqual({ maxRating: "3" });
  });

  it("fetchAdminUsers queries /v1/admin/users", async () => {
    const { api, lastCall } = createMockApi(() =>
      Promise.resolve({ items: [], total: 0, page: 1, pageSize: 20 }),
    );

    await fetchAdminUsers({ role: "user" }, api);
    expect(lastCall().url).toBe("/v1/admin/users");
    expect(lastCall().options?.query).toEqual({ role: "user" });
  });
});
