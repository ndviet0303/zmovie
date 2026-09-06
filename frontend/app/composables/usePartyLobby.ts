import { computed, onMounted, ref } from "vue";
import { fetchCatalogTitles } from "~/services/catalog.service";
import {
  createWatchParty,
  deleteWatchParty,
  fetchWatchParties,
} from "~/services/watch-party.service";
import type { TitleSummary } from "~/types/catalog";
import type { WatchPartyRoom } from "~/types/watch-party";

type LobbyTab = "latest" | "popular" | "mine";
const managementStorageKey = "zmovie.party.management";

export function usePartyLobby() {
  const router = useRouter();
  const { locale } = useLocale();
  const rooms = ref<WatchPartyRoom[]>([]);
  const titles = ref<TitleSummary[]>([]);
  const ownedTokens = ref<Record<string, string>>({});
  const activeTab = ref<LobbyTab>("latest");
  const selectedMovie = ref("");
  const selectedEpisode = ref(1);
  const pending = ref(true);
  const creating = ref(false);
  const error = ref("");

  const visibleRooms = computed(() => {
    const available =
      activeTab.value === "mine"
        ? rooms.value.filter((room) => Boolean(ownedTokens.value[room.roomId]))
        : [...rooms.value];
    return available.sort((left, right) => {
      if (activeTab.value === "popular")
        return right.activeUserCount - left.activeUserCount;
      return Date.parse(right.lastUpdated) - Date.parse(left.lastUpdated);
    });
  });

  const titleBySlug = computed<Record<string, TitleSummary>>(() =>
    Object.fromEntries(titles.value.map((title) => [title.slug, title])),
  );

  async function refresh() {
    pending.value = true;
    error.value = "";
    try {
      const [roomResponse, titleResponse] = await Promise.all([
        fetchWatchParties(),
        fetchCatalogTitles({ locale: locale.value, page: 1, pageSize: 100 }),
      ]);
      rooms.value = roomResponse;
      titles.value = titleResponse.items;
      selectedMovie.value ||= titleResponse.items[0]?.slug ?? "";
    } catch {
      error.value = "Không thể tải sảnh xem chung.";
    } finally {
      pending.value = false;
    }
  }

  async function createRoom() {
    if (!selectedMovie.value || creating.value) return;
    creating.value = true;
    error.value = "";
    try {
      const result = await createWatchParty(
        selectedMovie.value,
        Math.max(1, selectedEpisode.value),
      );
      ownedTokens.value[result.room.roomId] = result.managementToken;
      window.localStorage.setItem(
        managementStorageKey,
        JSON.stringify(ownedTokens.value),
      );
      await router.push({
        path: `/party/${result.room.roomId}`,
        query: {
          movie: result.room.movieSlug,
          episode: String(result.room.episodeNumber),
        },
      });
    } catch {
      error.value = "Không thể tạo phòng. Hãy chọn phim khác và thử lại.";
    } finally {
      creating.value = false;
    }
  }

  async function removeRoom(roomId: string) {
    const token = ownedTokens.value[roomId];
    if (!token) return;
    await deleteWatchParty(roomId, token);
    ownedTokens.value = Object.fromEntries(
      Object.entries(ownedTokens.value).filter(([id]) => id !== roomId),
    );
    window.localStorage.setItem(
      managementStorageKey,
      JSON.stringify(ownedTokens.value),
    );
    rooms.value = rooms.value.filter((room) => room.roomId !== roomId);
  }

  onMounted(() => {
    const stored = window.localStorage.getItem(managementStorageKey);
    if (stored) {
      try {
        ownedTokens.value = JSON.parse(stored) as Record<string, string>;
      } catch {
        window.localStorage.removeItem(managementStorageKey);
      }
    }
    void refresh();
  });

  useZMovieSeo({
    title: "Xem chung",
    description: "Tạo phòng, mời bạn bè và xem phim đồng bộ trên ZMovie.",
  });

  return {
    locale,
    rooms,
    visibleRooms,
    titles,
    titleBySlug,
    ownedTokens,
    activeTab,
    selectedMovie,
    selectedEpisode,
    pending,
    creating,
    error,
    refresh,
    createRoom,
    removeRoom,
  };
}
