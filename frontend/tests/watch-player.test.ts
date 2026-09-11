import { beforeEach, describe, expect, it } from "bun:test";
import {
  useWatchPlayer,
  type WatchPlayerDependencies,
} from "../app/composables/useWatchPlayer";
import type { PlaybackResponse, TitleDetail } from "../app/types/catalog";
import type { Library } from "../app/types/library";

// Mock localStorage and window in Bun test runner
let mockStorage: Record<string, string> = {};
const mockLocalStorage = {
  getItem: (key: string) => mockStorage[key] ?? null,
  setItem: (key: string, value: string) => {
    mockStorage[key] = String(value);
  },
  removeItem: (key: string) => {
    mockStorage = Object.fromEntries(
      Object.entries(mockStorage).filter(([k]) => k !== key),
    );
  },
  clear: () => {
    mockStorage = {};
  },
  key: () => null,
  length: 0,
};

globalThis.localStorage = mockLocalStorage;
(globalThis as unknown as { window: unknown }).window = {
  localStorage: mockLocalStorage,
  location: { search: "" },
};

const mockPlaybackResponse: PlaybackResponse = {
  slug: "demo-movie",
  title: "Demo Movie",
  isSeries: false,
  episodes: [
    {
      number: 1,
      name: "Tập 1",
      hlsUrl: "https://example.test/ep1.m3u8",
      sources: [
        {
          provider: "Primary",
          url: "https://example.test/source1.mp4",
          format: "video",
          priority: 1,
        },
        {
          provider: "Backup HLS",
          url: "https://example.test/source2.m3u8",
          format: "hls",
          priority: 2,
        },
        {
          provider: "Embed Fallback",
          url: "https://embed.example.test/ep1",
          format: "embed",
          priority: 3,
        },
      ],
    },
    {
      number: 2,
      name: "Tập 2",
      hlsUrl: "https://example.test/ep2.m3u8",
      sources: [
        {
          provider: "Primary",
          url: "https://example.test/ep2.mp4",
          format: "video",
          priority: 1,
        },
      ],
    },
  ],
};

const mockTitleDetail: TitleDetail = {
  id: "title-1",
  slug: "demo-movie",
  title: "Demo Movie",
  synopsis: "Synopsis",
  genre: "Action",
  year: 2026,
  type: "movie",
  posterUrl: "https://example.test/poster.jpg",
  runtimeMinutes: 120,
  featured: false,
  viewCount: 100,
  likeCount: 50,
  dislikeCount: 2,
  userReaction: null,
  isSaved: false,
  isR2Hosted: false,
  episodes: [],
  cast: [],
  directors: [],
  country: "Vietnam",
  ageRating: "P",
  ratingAverage: 8.5,
  ratingCount: 10,
  userRating: null,
};

function createVideoMock(
  initial: Partial<HTMLVideoElement> = {},
): HTMLVideoElement {
  return {
    currentTime: 0,
    duration: 120,
    paused: true,
    readyState: 4,
    volume: 1,
    muted: false,
    playbackRate: 1,
    pause: () => {},
    play: async () => {},
    load: () => {},
    removeAttribute: () => {},
    setAttribute: () => {},
    ...initial,
  } as unknown as HTMLVideoElement;
}

function createTestPlayer(
  slug: string,
  overrides: WatchPlayerDependencies = {},
) {
  return useWatchPlayer(slug, {
    fetchPlayback: async () => mockPlaybackResponse,
    fetchTitle: async () => mockTitleDetail,
    fetchLibrary: async () => null,
    ...overrides,
  });
}

describe("useWatchPlayer - Deterministic Startup, Resume & Recovery", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("resolves playback and selects first episode by default", async () => {
    const player = createTestPlayer("demo-movie");
    await player.loadData();

    expect(player.playback.value).toBeDefined();
    expect(player.selectedEpisodeIndex.value).toBe(0);
    expect(player.currentSource.value?.url).toBe(
      "https://example.test/source1.mp4",
    );
  });

  it("prioritizes remote history within 500ms budget to select episode", async () => {
    const mockLib: Library = {
      history: [
        {
          title: {
            slug: "demo-movie",
            title: "Demo Movie",
            posterUrl: "",
            year: 2026,
            type: "movie",
            genre: "Action",
            isR2Hosted: false,
          },
          episodeNumber: 2,
          progressSeconds: 120,
          updatedAt: new Date().toISOString(),
        },
      ],
      saved: [],
    };

    const player = createTestPlayer("demo-movie", {
      fetchLibrary: async () => mockLib,
    });
    await player.loadData();

    expect(player.selectedEpisodeIndex.value).toBe(1);
    expect(player.currentEpisode.value?.number).toBe(2);
  });

  it("lets explicit viewer selection supersede late history response", async () => {
    const { promise, resolve } = Promise.withResolvers<Library | null>();

    const player = createTestPlayer("demo-movie", {
      fetchLibrary: () => promise,
    });
    const loadPromise = player.loadData();

    // User explicitly selects Episode 1 while history is still pending
    player.selectEpisode(0);

    // Now resolve history afterwards with Episode 2
    resolve({
      history: [
        {
          title: {
            slug: "demo-movie",
            title: "Demo Movie",
            posterUrl: "",
            year: 2026,
            type: "movie",
            genre: "Action",
            isR2Hosted: false,
          },
          episodeNumber: 2,
          progressSeconds: 120,
          updatedAt: new Date().toISOString(),
        },
      ],
      saved: [],
    });

    await loadPromise;

    // Explicit selection of Episode 1 wins
    expect(player.selectedEpisodeIndex.value).toBe(0);
    expect(player.currentEpisode.value?.number).toBe(1);
  });

  it("handles history rejection gracefully without blocking playback", async () => {
    const { promise, reject } = Promise.withResolvers<Library | null>();

    const player = createTestPlayer("demo-movie", {
      fetchLibrary: () => promise,
    });
    const loadPromise = player.loadData();

    // History fails
    reject(new Error("Network failure"));
    await loadPromise;

    // Defaults to episode 0 and playback proceeds without terminal error
    expect(player.selectedEpisodeIndex.value).toBe(0);
    expect(player.playerError.value).toBe("");
  });

  it("validates resume positions and ignores positions <= 5s", async () => {
    localStorage.setItem(
      "zmovie.watch-progress.v1",
      JSON.stringify({
        "demo-movie#1": {
          episodeNumber: 1,
          progressSeconds: 3,
          updatedAt: Date.now(),
        },
      }),
    );

    const player = createTestPlayer("demo-movie");
    await player.loadData();

    const videoMock = createVideoMock({
      currentTime: 0,
      duration: 100,
      paused: true,
    });

    player.video.value = videoMock;
    player.onLoadedMetadata();

    // Position <= 5 should not seek
    expect(videoMock.currentTime).toBe(0);
  });

  it("applies valid resume position once per generation and does not re-apply", async () => {
    localStorage.setItem(
      "zmovie.watch-progress.v1",
      JSON.stringify({
        "demo-movie#1": {
          episodeNumber: 1,
          progressSeconds: 45,
          updatedAt: Date.now(),
        },
      }),
    );

    const player = createTestPlayer("demo-movie");
    await player.loadData();

    const videoMock = createVideoMock({
      currentTime: 0,
      duration: 120,
      paused: true,
    });

    player.video.value = videoMock;
    player.onLoadedMetadata();

    // Resume of 45s applied
    expect(videoMock.currentTime).toBe(45);

    // Later metadata updates should not reset or re-apply position
    videoMock.currentTime = 90;
    player.onLoadedMetadata();
    expect(videoMock.currentTime).toBe(90);
  });

  it("seeking while paused preserves paused intent without accidental autoplay", async () => {
    const player = createTestPlayer("demo-movie");
    await player.loadData();

    let playCalled = false;
    const videoMock = createVideoMock({
      currentTime: 10,
      duration: 120,
      paused: true,
      play: async () => {
        playCalled = true;
      },
    });

    player.video.value = videoMock;

    player.seek(50);
    expect(videoMock.currentTime).toBe(50);
    expect(player.isLoading.value).toBe(true);

    player.onSeeked();
    expect(player.isLoading.value).toBe(false);
    expect(playCalled).toBe(false);
    expect(player.isPlaying.value).toBe(false);
  });

  it("consecutive rapid seeks settle at the latest target", async () => {
    const player = createTestPlayer("demo-movie");
    await player.loadData();

    const videoMock = createVideoMock({
      currentTime: 10,
      duration: 120,
      paused: true,
    });

    player.video.value = videoMock;

    player.seek(20);
    player.seek(40);
    player.seek(75);

    expect(videoMock.currentTime).toBe(75);
  });

  it("fails over to backup source on native video error without page reload", async () => {
    const player = createTestPlayer("demo-movie");
    await player.loadData();

    expect(player.activeSourceIndex.value).toBe(0);
    expect(player.currentSource.value?.format).toBe("video");

    // Simulate native video error on source 0
    player.onNativeVideoError();

    // Advances to source 1 (Backup HLS)
    expect(player.activeSourceIndex.value).toBe(1);
    expect(player.currentSource.value?.provider).toBe("Backup HLS");
    expect(player.currentSource.value?.format).toBe("hls");
  });

  it("enters terminal error state when all sources are exhausted and supports manual retry", async () => {
    const player = createTestPlayer("demo-movie");
    await player.loadData();

    // Error on source 0 -> advances to source 1
    player.onNativeVideoError();
    expect(player.activeSourceIndex.value).toBe(1);

    // Error on source 1 -> advances to source 2 (Embed)
    player.onNativeVideoError();
    expect(player.activeSourceIndex.value).toBe(2);

    // Error on source 2 -> exhausted
    player.onNativeVideoError();
    expect(player.playerError.value).toContain(
      "Tất cả nguồn phát đều không khả dụng",
    );
    expect(player.isLoading.value).toBe(false);

    // Manual retry resets to primary source
    player.retryPlayback();
    expect(player.activeSourceIndex.value).toBe(0);
    expect(player.playerError.value).toBe("");
  });
});
