import { describe, expect, it } from "bun:test";
import {
  getPlaybackSourceKind,
  inferPlaybackFormat,
  isDirectVideoUrl,
} from "../app/utils/playback";

describe("playback source classification", () => {
  it("recognizes direct video URLs even with query strings", () => {
    expect(
      isDirectVideoUrl("https://cdn.example.com/movie.MP4?token=abc"),
    ).toBe(true);
    expect(inferPlaybackFormat("https://cdn.example.com/movie.webm#t=10")).toBe(
      "video",
    );
  });

  it("keeps HLS manifests on the HLS path", () => {
    expect(
      getPlaybackSourceKind({
        url: "https://cdn.example.com/playlist.m3u8?token=abc",
        format: "embed",
      }),
    ).toBe("hls");
  });

  it("treats stale embed metadata as native video for direct media", () => {
    expect(
      getPlaybackSourceKind({
        url: "https://cdn.example.com/movie.mp4",
        format: "embed",
      }),
    ).toBe("video");
  });

  it("keeps real embed URLs in iframe mode", () => {
    expect(
      getPlaybackSourceKind({
        url: "https://player.example.com/embed/movie-1",
        format: "embed",
      }),
    ).toBe("embed");
  });
});
