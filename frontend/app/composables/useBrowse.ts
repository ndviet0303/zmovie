import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { fetchCatalogTitles } from "~/services/catalog.service";
import { fetchPersonalizedDiscovery } from "~/services/discovery.service";
import { searchCatalogTitles } from "~/services/search.service";
import type {
  SortOrder,
  TitleListResponse,
  TitleSummary,
} from "~/types/catalog";

function splitGenres(genre: string): string[] {
  return genre
    .split(",")
    .map((item) => item.trim())
    .filter(Boolean);
}

export function useBrowse() {
  const route = useRoute();
  const { locale, messages, setLocale: setGlobalLocale } = useLocale();

  const query = ref("");
  const selectedGenre = ref(
    typeof route.query.genre === "string" ? route.query.genre : "all",
  );
  const filtersOpen = ref(false);
  const sortOrder = ref<SortOrder>("latest");

  const selectedType = computed(() =>
    route.query.type === "series" ? "series" : "all",
  );
  const collection = computed(() =>
    route.query.collection === "recommended" ? "recommended" : "catalog",
  );
  const isRecommended = computed(() => collection.value === "recommended");

  const data = ref<TitleListResponse>();
  const isLoading = ref(true);
  const loadError = ref(false);

  async function loadBrowseData(requestedLocale = locale.value) {
    const catalog = await fetchCatalogTitles({ locale: requestedLocale });

    if (isRecommended.value) {
      try {
        const personalized = await fetchPersonalizedDiscovery(requestedLocale);
        if (personalized.recommended.length) {
          return {
            items: personalized.recommended,
            total: personalized.recommended.length,
          };
        }
      } catch {
        // Guests fall back to catalog
      }
    }

    return catalog;
  }

  async function refreshBrowseData(requestedLocale = locale.value) {
    isLoading.value = true;
    loadError.value = false;
    try {
      data.value = await loadBrowseData(requestedLocale);
    } catch {
      data.value = { items: [], total: 0 };
      loadError.value = true;
    } finally {
      isLoading.value = false;
    }
  }

  let searchTimer: ReturnType<typeof setTimeout> | undefined;

  watch(query, (value) => {
    clearTimeout(searchTimer);
    searchTimer = setTimeout(async () => {
      isLoading.value = true;
      loadError.value = false;
      try {
        data.value = value.trim()
          ? await searchCatalogTitles({
              q: value.trim(),
              locale: locale.value,
            })
          : await loadBrowseData();
      } catch {
        data.value = { items: [], total: 0 };
        loadError.value = true;
      } finally {
        isLoading.value = false;
      }
    }, 180);
  });

  onMounted(() => {
    void refreshBrowseData();
  });

  onBeforeUnmount(() => {
    clearTimeout(searchTimer);
  });

  const genres = computed(() => [
    "all",
    ...new Set(
      data.value?.items.flatMap((title) => splitGenres(title.genre)) ?? [],
    ),
  ]);

  const visibleTitles = computed<TitleSummary[]>(() => {
    const filtered = (data.value?.items ?? []).filter((title) => {
      const matchesGenre =
        selectedGenre.value === "all" ||
        splitGenres(title.genre).includes(selectedGenre.value);
      const matchesType =
        selectedType.value === "all" || title.type === selectedType.value;
      return matchesGenre && matchesType;
    });

    if (isRecommended.value) return filtered;

    return filtered.sort((a, b) => {
      if (sortOrder.value === "oldest") return Number(a.year) - Number(b.year);
      if (sortOrder.value === "title") return a.title.localeCompare(b.title);
      return Number(b.year) - Number(a.year);
    });
  });

  const activeFilterCount = computed(() =>
    selectedGenre.value === "all" ? 0 : 1,
  );

  function clearFilters() {
    selectedGenre.value = "all";
  }

  async function changeLocale(nextLocale: "vi" | "en") {
    if (nextLocale === locale.value) return;
    await refreshBrowseData(nextLocale);
    if (!loadError.value) {
      setGlobalLocale(nextLocale);
    }
  }

  const copy = computed(() => {
    const b = messages.value.browse;
    return {
      title: isRecommended.value
        ? b.recommendedTitle
        : selectedType.value === "series"
          ? b.seriesTitle
          : route.query.sort === "latest"
            ? b.latestTitle
            : b.title,
      placeholder: b.placeholder,
      filters: b.filters,
      all: b.all,
      movies: b.movies,
      series: b.series,
      latest: b.latest,
      showMore: b.showMore,
      loading: b.loading,
      error: b.error,
      empty: b.empty,
      chooseGenre: b.chooseGenre,
      clearFilters: b.clearFilters,
      showResults: b.showResults,
      titlesCount: b.titlesCount,
    };
  });

  function genreLabel(genre: string) {
    return genre === "all" ? copy.value.all : genre;
  }

  useZMovieSeo({
    title: computed(() => copy.value.title),
    description: computed(() => messages.value.browse.seoDescription),
  });

  return {
    locale,
    query,
    selectedGenre,
    filtersOpen,
    sortOrder,
    isRecommended,
    isLoading,
    loadError,
    genres,
    visibleTitles,
    activeFilterCount,
    copy,
    genreLabel,
    clearFilters,
    changeLocale,
  };
}
