import { computed, onMounted, ref } from "vue";
import type { TitleSummary } from "~/types/catalog";
import type {
  ContinueWatching,
  PersonalizedDiscovery,
  TopPeriod,
} from "~/types/discovery";
import {
  fetchHomeDiscovery,
  fetchPersonalizedDiscovery,
  fetchTopTitles,
} from "~/services/discovery.service";

function takeUniqueTitles(
  titles: TitleSummary[],
  excludedSlugs: ReadonlySet<string>,
  limit = 5,
): TitleSummary[] {
  const seen = new Set(excludedSlugs);
  return titles.filter((title) => {
    if (seen.has(title.slug) || seen.size >= excludedSlugs.size + limit)
      return false;
    seen.add(title.slug);
    return true;
  });
}

export async function useHomePage() {
  const {
    locale,
    messages,
    isVietnamese,
    setLocale: setGlobalLocale,
  } = useLocale();
  const activeLocale = ref(locale.value);

  const topPeriod = ref<TopPeriod>("week");
  const topPeriods: TopPeriod[] = ["day", "week", "month"];

  const homePromise = useAsyncData("discovery-home", () =>
    fetchHomeDiscovery(activeLocale.value),
  );
  const topPromise = useAsyncData("discovery-top", () =>
    fetchTopTitles(topPeriod.value, {
      locale: activeLocale.value,
      limit: 10,
    }),
  );

  useZMovieSeo({
    title: computed(() =>
      isVietnamese.value ? "Xem phim hay online" : "Watch great movies online",
    ),
    description: computed(() => messages.value.home.description),
    image: computed(() => homePromise.data.value?.hero.posterUrl),
  });

  onMounted(() => {
    void loadPersonalized(activeLocale.value);
  });

  const [
    { data: home, error },
    { data: topTitles, pending: topPending, refresh: refreshTop },
  ] = await Promise.all([homePromise, topPromise]);

  const catalogTitles = computed(() => home.value?.trending ?? []);
  const personalized = ref<PersonalizedDiscovery | null>(null);

  const continueWatching = computed(
    () => personalized.value?.continueWatching ?? [],
  );

  const recommendedTitles = computed(() =>
    personalized.value?.recommended.length
      ? personalized.value.recommended
      : takeUniqueTitles(catalogTitles.value, new Set()),
  );

  const recommendedSlugs = computed(
    () => new Set(recommendedTitles.value.map((title) => title.slug)),
  );

  const newReleaseTitles = computed(() =>
    takeUniqueTitles(
      [...catalogTitles.value].sort((a, b) => Number(b.year) - Number(a.year)),
      recommendedSlugs.value,
    ),
  );

  const newReleaseSlugs = computed(
    () => new Set(newReleaseTitles.value.map((title) => title.slug)),
  );

  const titles2026 = computed(() =>
    takeUniqueTitles(
      catalogTitles.value.filter((title) => Number(title.year) === 2026),
      new Set([...recommendedSlugs.value, ...newReleaseSlugs.value]),
    ),
  );

  const titles2026Slugs = computed(
    () => new Set(titles2026.value.map((title) => title.slug)),
  );

  const moviePicks = computed(() =>
    takeUniqueTitles(
      catalogTitles.value.filter((title) => title.type === "movie"),
      new Set([
        ...recommendedSlugs.value,
        ...newReleaseSlugs.value,
        ...titles2026Slugs.value,
      ]),
    ),
  );

  const moviePickSlugs = computed(
    () => new Set(moviePicks.value.map((title) => title.slug)),
  );

  const seriesPicks = computed(() =>
    takeUniqueTitles(
      catalogTitles.value.filter((title) => title.type === "series"),
      new Set([
        ...recommendedSlugs.value,
        ...newReleaseSlugs.value,
        ...titles2026Slugs.value,
        ...moviePickSlugs.value,
      ]),
    ),
  );

  async function loadPersonalized(targetLocale: "vi" | "en") {
    try {
      personalized.value = await fetchPersonalizedDiscovery(targetLocale);
    } catch {
      personalized.value = null;
    }
  }

  async function changeLocale(nextLocale: "vi" | "en") {
    if (nextLocale === activeLocale.value) return;
    try {
      const nextHome = await fetchHomeDiscovery(nextLocale);
      home.value = nextHome;
      activeLocale.value = nextLocale;
      setGlobalLocale(nextLocale);
      await refreshTop();
      await loadPersonalized(nextLocale);
    } catch {
      // Keep existing catalog on failure
    }
  }

  function selectTopPeriod(period: TopPeriod) {
    if (period === topPeriod.value) return;
    topPeriod.value = period;
    void refreshTop();
  }

  function formatViews(count: number) {
    if (count >= 1_000_000)
      return `${(count / 1_000_000).toFixed(count >= 10_000_000 ? 0 : 1)}M`;
    if (count >= 1_000)
      return `${(count / 1_000).toFixed(count >= 10_000 ? 0 : 1)}K`;
    return String(count);
  }

  function progressPercent(item: ContinueWatching) {
    const title = item.title;
    return Math.min(
      100,
      Math.max(
        3,
        Math.round(
          (item.progressSeconds /
            Math.max(title.type === "series" ? 45 * 60 : 120 * 60, 1)) *
            100,
        ),
      ),
    );
  }

  return {
    home,
    error,
    activeLocale,
    messages,
    isVietnamese,
    continueWatching,
    recommendedTitles,
    newReleaseTitles,
    titles2026,
    moviePicks,
    seriesPicks,
    topPeriod,
    topPeriods,
    topTitles,
    topPending,
    changeLocale,
    selectTopPeriod,
    formatViews,
    progressPercent,
  };
}
