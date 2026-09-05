type PlaybackSourceLike = {
  url: string;
  format: string;
};

const DIRECT_VIDEO_EXTENSIONS = [".mp4", ".webm", ".ogg", ".m4v"];

function urlPathname(url: string): string {
  try {
    return new URL(url, "http://localhost").pathname.toLowerCase();
  } catch {
    return url.split(/[?#]/, 1)[0].toLowerCase();
  }
}

export function isDirectVideoUrl(url: string): boolean {
  const pathname = urlPathname(url);
  return DIRECT_VIDEO_EXTENSIONS.some((extension) =>
    pathname.endsWith(extension),
  );
}

export type PlaybackSourceKind = "hls" | "video" | "embed";

export function getPlaybackSourceKind(
  source: PlaybackSourceLike | null | undefined,
): PlaybackSourceKind {
  if (!source?.url) return "embed";

  const pathname = urlPathname(source.url);
  if (isDirectVideoUrl(source.url)) return "video";
  if (pathname.endsWith(".m3u8") || source.format.toLowerCase() === "hls") {
    return "hls";
  }
  if (source.format.toLowerCase() === "video") return "video";
  return "embed";
}

export function inferPlaybackFormat(url: string): PlaybackSourceKind {
  return getPlaybackSourceKind({ url, format: "" });
}
