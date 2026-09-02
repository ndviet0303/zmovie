import { describe, expect, it } from "bun:test";
import {
  fetchCatalogGenres,
  fetchCatalogPlayback,
  fetchCatalogTitleBySlug,
  fetchCatalogTitles,
  fetchTitleReviews,
  recordTitleView,
} from "../app/services/catalog.service";
import { createMockApi } from "./test-utils";

describe("catalog.service", () => {
  it("fetchCatalogTitles queries /v1/catalog/titles with params", async () => {
    const { api, lastCall } = createMockApi(() =>
      Promise.resolve({
        items: [{ slug: "movie-1", title: "Movie 1" }],
        total: 1,
        page: 1,
        pageSize: 24,
      }),
    );

    const res = await fetchCatalogTitles(
      { genre: "action", locale: "vi" },
      api,
    );
    expect(lastCall().url).toBe("/v1/catalog/titles");
    expect(lastCall().options?.query).toEqual({
      genre: "action",
      locale: "vi",
    });
    expect(res.items.length).toBe(1);
  });

  it("fetchCatalogTitleBySlug queries encoded slug with locale", async () => {
    const { api, lastCall } = createMockApi(() =>
      Promise.resolve({
        slug: "movie-test",
        title: "Test Movie",
      }),
    );

    const res = await fetchCatalogTitleBySlug("movie/special", "en", api);
    expect(lastCall().url).toBe("/v1/catalog/titles/movie%2Fspecial");
    expect(lastCall().options?.query).toEqual({ locale: "en" });
    expect(res.slug).toBe("movie-test");
  });

  it("fetchCatalogGenres queries /v1/catalog/genres", async () => {
    const { api, lastCall } = createMockApi(() =>
      Promise.resolve(["Action", "Drama"]),
    );

    const res = await fetchCatalogGenres(api);
    expect(lastCall().url).toBe("/v1/catalog/genres");
    expect(res).toEqual(["Action", "Drama"]);
  });

  it("fetchCatalogPlayback queries /v1/catalog/titles/:slug/playback", async () => {
    const { api, lastCall } = createMockApi(() =>
      Promise.resolve({
        hlsUrl: "https://cdn.example.com/stream.m3u8",
        isSeries: false,
        episodes: [],
      }),
    );

    const res = await fetchCatalogPlayback("my-movie", "vi", api);
    expect(lastCall().url).toBe("/v1/catalog/titles/my-movie/playback");
    expect(res.isSeries).toBe(false);
  });

  it("fetchTitleReviews queries reviews endpoint", async () => {
    const { api, lastCall } = createMockApi(() =>
      Promise.resolve({
        averageRating: 4.8,
        ratingCount: 10,
        items: [],
      }),
    );

    const res = await fetchTitleReviews("my-movie", api);
    expect(lastCall().url).toBe("/v1/catalog/titles/my-movie/reviews");
    expect(res.averageRating).toBe(4.8);
  });

  it("recordTitleView posts view tracking payload with credentials", async () => {
    const { api, lastCall } = createMockApi(() =>
      Promise.resolve({
        viewCount: 42,
        counted: true,
      }),
    );

    const res = await recordTitleView("my-movie", 3, api);
    expect(lastCall().url).toBe("/v1/catalog/titles/my-movie/views");
    expect(lastCall().options?.method).toBe("POST");
    expect(lastCall().options?.credentials).toBe("include");
    expect(lastCall().options?.body).toEqual({ episodeNumber: 3 });
    expect(res.viewCount).toBe(42);
  });
});
