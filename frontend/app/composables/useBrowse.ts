import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { fetchCatalogTitles } from "~/services/catalog.service";
import { fetchPersonalizedDiscovery } from "~/services/discovery.service";
import type { SortOrder, TitleListResponse } from "~/types/catalog";

export function useBrowse() {
  const route = useRoute();
  const router = useRouter();
  const { locale, messages, setLocale: setGlobalLocale } = useLocale();

  const query = ref(
    typeof route.query.query === "string" ? route.query.query : "",
  );
  const selectedGenre = ref(
    typeof route.query.genre === "string" ? route.query.genre : "all",
  );
  const selectedCountry = ref(
    typeof route.query.country === "string" ? route.query.country : "all",
  );
  const selectedYear = ref(
    typeof route.query.year === "string" ? route.query.year : "all",
  );
  const selectedFormat = ref(
    typeof route.query.format === "string"
      ? route.query.format
      : typeof route.query.type === "string"
        ? route.query.type
        : "all",
  );
  const filtersOpen = ref(false);
  const sortOrder = ref<SortOrder>(
    route.query.sort === "oldest" || route.query.sort === "title"
      ? route.query.sort
      : "latest",
  );
  const page = ref(Math.max(1, Number(route.query.page) || 1));
  const pageSize = 30;
  const collection = computed(() =>
    route.query.collection === "recommended" ? "recommended" : "catalog",
  );
  const isRecommended = computed(() => collection.value === "recommended");

  const data = ref<TitleListResponse>();
  const isLoading = ref(true);
  const loadError = ref(false);

  async function loadBrowseData(requestedLocale = locale.value) {
    if (isRecommended.value) {
      try {
        const personalized = await fetchPersonalizedDiscovery(requestedLocale);
        if (personalized.recommended.length) {
          const offset = (page.value - 1) * pageSize;
          return {
            items: personalized.recommended.slice(offset, offset + pageSize),
            total: personalized.recommended.length,
          };
        }
      } catch {
        // Guests fall back to catalog.
      }
    }

    return fetchCatalogTitles({
      q: query.value.trim() || undefined,
      genre: selectedGenre.value === "all" ? undefined : selectedGenre.value,
      country:
        selectedCountry.value === "all" ? undefined : selectedCountry.value,
      year:
        selectedYear.value === "all" ? undefined : Number(selectedYear.value),
      type: selectedFormat.value === "all" ? undefined : selectedFormat.value,
      sort: sortOrder.value,
      page: page.value,
      pageSize,
      locale: requestedLocale,
    });
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

  let refreshTimer: number | undefined;
  let routeTimer: number | undefined;

  watch(
    [
      query,
      selectedGenre,
      selectedCountry,
      selectedYear,
      selectedFormat,
      sortOrder,
      page,
    ],
    () => {
      if (!import.meta.client) return;
      clearTimeout(refreshTimer);
      refreshTimer = window.setTimeout(() => {
        void refreshBrowseData();
      }, 180);
    },
  );

  onMounted(() => {
    void refreshBrowseData();
  });

  onBeforeUnmount(() => {
    clearTimeout(refreshTimer);
    clearTimeout(routeTimer);
  });

  const years = [
    "all",
    ...Array.from({ length: new Date().getFullYear() - 1970 + 1 }, (_, index) =>
      String(new Date().getFullYear() - index),
    ),
  ];
  const totalResults = computed(() => Number(data.value?.total ?? 0));
  const totalPages = computed(() =>
    Math.max(1, Math.ceil(totalResults.value / pageSize)),
  );
  const visibleTitles = computed(() => data.value?.items ?? []);

  watch(
    [
      query,
      selectedGenre,
      selectedCountry,
      selectedYear,
      selectedFormat,
      sortOrder,
    ],
    () => {
      page.value = 1;
    },
  );

  watch(
    [
      query,
      selectedGenre,
      selectedCountry,
      selectedYear,
      selectedFormat,
      sortOrder,
      page,
    ],
    () => {
      if (!import.meta.client) return;
      clearTimeout(routeTimer);
      routeTimer = window.setTimeout(() => {
        void router.replace({
          query: {
            ...route.query,
            query: query.value.trim() || undefined,
            genre:
              selectedGenre.value === "all" ? undefined : selectedGenre.value,
            country:
              selectedCountry.value === "all"
                ? undefined
                : selectedCountry.value,
            year: selectedYear.value === "all" ? undefined : selectedYear.value,
            format:
              selectedFormat.value === "all" ? undefined : selectedFormat.value,
            type: undefined,
            sort: sortOrder.value === "latest" ? undefined : sortOrder.value,
            page: page.value > 1 ? String(page.value) : undefined,
          },
        });
      }, 100);
    },
  );

  function goToPage(nextPage: number) {
    page.value = Math.max(1, Math.min(nextPage, totalPages.value));
    if (import.meta.client) window.scrollTo({ top: 0, behavior: "smooth" });
  }

  const activeFilterCount = computed(() => {
    let count = 0;
    if (selectedGenre.value !== "all") count++;
    if (selectedCountry.value !== "all") count++;
    if (selectedYear.value !== "all") count++;
    if (selectedFormat.value !== "all") count++;
    return count;
  });

  function clearFilters() {
    selectedGenre.value = "all";
    selectedCountry.value = "all";
    selectedYear.value = "all";
    selectedFormat.value = "all";
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
        : selectedFormat.value === "series"
          ? b.seriesTitle
          : selectedFormat.value === "movie"
            ? b.movies
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
    selectedCountry,
    selectedYear,
    selectedFormat,
    filtersOpen,
    page,
    pageSize,
    totalResults,
    totalPages,
    sortOrder,
    isRecommended,
    isLoading,
    loadError,
    years,
    visibleTitles,
    activeFilterCount,
    copy,
    genreLabel,
    clearFilters,
    changeLocale,
    goToPage,
  };
}
