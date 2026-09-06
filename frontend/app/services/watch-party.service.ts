import type { ApiFetch } from "~/types/api-fetch";
import type {
  CreatedWatchPartyRoom,
  WatchPartyRoom,
} from "~/types/watch-party";

function resolveApi(api?: ApiFetch): ApiFetch {
  return api ?? (useNuxtApp().$api as ApiFetch);
}

export function fetchWatchParties(api?: ApiFetch): Promise<WatchPartyRoom[]> {
  return resolveApi(api)<WatchPartyRoom[]>("/v1/watch-parties");
}

export function createWatchParty(
  movieSlug: string,
  episodeNumber: number,
  api?: ApiFetch,
): Promise<CreatedWatchPartyRoom> {
  return resolveApi(api)<CreatedWatchPartyRoom>("/v1/watch-parties", {
    method: "POST",
    body: { movieSlug, episodeNumber },
  });
}

export function deleteWatchParty(
  roomId: string,
  managementToken: string,
  api?: ApiFetch,
): Promise<void> {
  return resolveApi(api)(`/v1/watch-parties/${encodeURIComponent(roomId)}`, {
    method: "DELETE",
    query: { managementToken },
  }).then(() => undefined);
}
