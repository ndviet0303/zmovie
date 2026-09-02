import { describe, expect, it } from "bun:test";
import {
  fetchHomeDiscovery,
  fetchPersonalizedDiscovery,
  fetchTopTitles,
} from "../app/services/discovery.service";
import { createMockApi } from "./test-utils";

describe("discovery.service", () => {
  it("fetchHomeDiscovery queries /v1/discovery/home with locale", async () => {
    const { api, lastCall } = createMockApi(() =>
      Promise.resolve({
        featured: [],
        top: [],
        continueWatching: [],
      }),
    );

    await fetchHomeDiscovery("vi", api);
    expect(lastCall().url).toBe("/v1/discovery/home");
    expect(lastCall().options?.query).toEqual({ locale: "vi" });
  });

  it("fetchTopTitles queries /v1/discovery/top with period and limit", async () => {
    const { api, lastCall } = createMockApi(() => Promise.resolve([]));

    await fetchTopTitles("day", { limit: 5, locale: "en" }, api);
    expect(lastCall().url).toBe("/v1/discovery/top/day");
    expect(lastCall().options?.query).toEqual({ limit: 5, locale: "en" });
  });

  it("fetchPersonalizedDiscovery passes credentials and locale", async () => {
    const { api, lastCall } = createMockApi(() =>
      Promise.resolve({ continueWatching: [], recommended: [] }),
    );

    const res = await fetchPersonalizedDiscovery("vi", api);
    expect(lastCall().url).toBe("/v1/discovery/for-you");
    expect(lastCall().options?.credentials).toBe("include");
    expect(lastCall().options?.query).toEqual({ locale: "vi" });
    expect(res).toEqual({ continueWatching: [], recommended: [] });
  });
});
