import { computed, onMounted, ref } from "vue";
import {
  clearWatchHistory,
  fetchUserLibrary,
  removeWatchHistoryItem,
} from "~/services/library.service";
import type {
  HistoryItem,
  Library,
  LibraryTab,
  LibraryTitle,
} from "~/types/library";

function getErrorStatus(error: unknown): number | undefined {
  const fetchError = error as {
    status?: number;
    statusCode?: number;
    response?: { status?: number };
  };
  return (
    fetchError.status ?? fetchError.statusCode ?? fetchError.response?.status
  );
}

export function useMyList() {
  const { locale, messages } = useLocale();
  const library = ref<Library | null>(null);
  const loading = ref(true);
  const loadError = ref("");
  const activeTab = ref<LibraryTab>("saved");

  const items = computed<(LibraryTitle | HistoryItem)[]>(() =>
    activeTab.value === "saved"
      ? (library.value?.saved ?? [])
      : (library.value?.history ?? []),
  );

  function progress(item: HistoryItem) {
    const runtime = item.title.runtimeMinutes ?? 90;
    return Math.min(
      100,
      Math.round((item.progressSeconds / Math.max(runtime * 60, 1)) * 100),
    );
  }

  async function loadData() {
    loading.value = true;
    loadError.value = "";
    try {
      library.value = await fetchUserLibrary(locale.value);
    } catch (error: unknown) {
      const status = getErrorStatus(error);
      if (status === 401) {
        await navigateTo("/login");
        return;
      }
      loadError.value =
        status === 404
          ? messages.value.myList.errorNotFound
          : messages.value.myList.errorDefault;
    } finally {
      loading.value = false;
    }
  }

  async function removeFromHistory(slug: string) {
    if (!library.value) return;
    try {
      await removeWatchHistoryItem(slug);
      library.value.history = library.value.history.filter(
        (item) => item.title.slug !== slug,
      );
    } catch {
      // ignore
    }
  }

  async function clearAllHistory() {
    if (!library.value) return;
    try {
      await clearWatchHistory();
      library.value.history = [];
    } catch {
      // ignore
    }
  }

  return {
    locale,
    library,
    loading,
    loadError,
    activeTab,
    items,
    messages,
    progress,
    retryLoad: loadData,
    removeFromHistory,
    clearAllHistory,
  };
}
