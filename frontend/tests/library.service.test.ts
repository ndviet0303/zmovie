import { describe, expect, it } from "bun:test";
import {
  fetchUserLibrary,
  recordWatchHistory,
  removeTitleFromLibrary,
  saveTitleToLibrary,
  submitTitleReview,
} from "../app/services/library.service";
import { createMockApi } from "./test-utils";

describe("library.service", () => {
  it("fetchUserLibrary queries /v1/me/library with credentials", async () => {
    const { api, lastCall } = createMockApi(() =>
      Promise.resolve({
        saved: [],
        history: [],
      }),
    );

    await fetchUserLibrary("vi", api);
    expect(lastCall().url).toBe("/v1/me/library");
    expect(lastCall().options?.credentials).toBe("include");
    expect(lastCall().options?.query).toEqual({ locale: "vi" });
  });

  it("saveTitleToLibrary sends PUT to /v1/me/saved/:slug", async () => {
    const { api, lastCall } = createMockApi(() => Promise.resolve());

    await saveTitleToLibrary("my-slug", api);
    expect(lastCall().url).toBe("/v1/me/saved/my-slug");
    expect(lastCall().options?.method).toBe("PUT");
    expect(lastCall().options?.credentials).toBe("include");
  });

  it("removeTitleFromLibrary sends DELETE to /v1/me/saved/:slug", async () => {
    const { api, lastCall } = createMockApi(() => Promise.resolve());

    await removeTitleFromLibrary("my-slug", api);
    expect(lastCall().url).toBe("/v1/me/saved/my-slug");
    expect(lastCall().options?.method).toBe("DELETE");
  });

  it("recordWatchHistory posts progress with keepalive flag", async () => {
    const { api, lastCall } = createMockApi(() => Promise.resolve());

    await recordWatchHistory(
      "my-series",
      { episodeNumber: 2, progressSeconds: 120 },
      true,
      api,
    );
    expect(lastCall().url).toBe("/v1/me/history/my-series");
    expect(lastCall().options?.method).toBe("POST");
    expect(lastCall().options?.keepalive).toBe(true);
    expect(lastCall().options?.body).toEqual({
      episodeNumber: 2,
      progressSeconds: 120,
    });
  });

  it("submitTitleReview sends PUT to /v1/me/titles/:slug/review", async () => {
    const { api, lastCall } = createMockApi(() => Promise.resolve());

    await submitTitleReview(
      "my-movie",
      { rating: 5, comment: "Great film!" },
      api,
    );
    expect(lastCall().url).toBe("/v1/me/titles/my-movie/review");
    expect(lastCall().options?.method).toBe("PUT");
    expect(lastCall().options?.body).toEqual({
      rating: 5,
      comment: "Great film!",
    });
  });
});
