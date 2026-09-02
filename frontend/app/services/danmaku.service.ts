import type { ApiFetch } from "~/types/api-fetch";
import type { DanmakuItem } from "~/types/danmaku";

function resolveApi(api?: ApiFetch): ApiFetch {
  if (api) return api;
  const nuxtApp = useNuxtApp();
  return nuxtApp.$api as ApiFetch;
}

export async function fetchTimedDanmaku(
  slug: string,
  episodeNumber: number,
  from?: number,
  to?: number,
  api?: ApiFetch,
): Promise<DanmakuItem[]> {
  const client = resolveApi(api);
  const query: Record<string, string> = {};
  if (from !== undefined) query.from = String(from);
  if (to !== undefined) query.to = String(to);
  const res = await client<DanmakuItem[]>(
    `/v1/catalog/titles/${slug}/episodes/${episodeNumber}/danmaku`,
    {
      query,
    },
  );
  return res || [];
}

export async function sendDanmakuComment(
  slug: string,
  episodeNumber: number,
  payload: {
    timeSeconds: number;
    content: string;
    color?: string;
    authorName?: string;
  },
  api?: ApiFetch,
): Promise<DanmakuItem> {
  const client = resolveApi(api);
  return await client<DanmakuItem>(
    `/v1/catalog/titles/${slug}/episodes/${episodeNumber}/danmaku`,
    {
      method: "POST",
      body: payload,
    },
  );
}
