import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { fetchCatalogPeople } from "~/services/catalog.service";
import type { PersonSummary } from "~/types/catalog";

export function usePeople() {
  const route = useRoute();
  const router = useRouter();
  const { locale } = useLocale();
  const query = ref(
    typeof route.query.query === "string" ? route.query.query : "",
  );
  const people = ref<PersonSummary[]>([]);
  const total = ref(0);
  const page = ref(1);
  const pending = ref(true);
  const error = ref(false);
  let searchTimer: number | undefined;

  const hasMore = computed(() => people.value.length < total.value);

  async function load(reset = false) {
    if (reset) {
      page.value = 1;
      people.value = [];
    }
    pending.value = true;
    error.value = false;
    try {
      const response = await fetchCatalogPeople({
        q: query.value.trim() || undefined,
        page: page.value,
        pageSize: 30,
      });
      people.value = reset
        ? response.items
        : [...people.value, ...response.items];
      total.value = response.total;
    } catch {
      error.value = true;
    } finally {
      pending.value = false;
    }
  }

  async function loadMore() {
    if (pending.value || !hasMore.value) return;
    page.value += 1;
    await load();
  }

  watch(query, () => {
    if (!import.meta.client) return;
    clearTimeout(searchTimer);
    searchTimer = window.setTimeout(() => {
      void router.replace({
        query: { ...route.query, query: query.value.trim() || undefined },
      });
      void load(true);
    }, 220);
  });

  onMounted(() => {
    void load(true);
  });
  onBeforeUnmount(() => clearTimeout(searchTimer));

  useZMovieSeo({
    title: "Danh bạ nghệ sĩ",
    description:
      "Tìm diễn viên, đạo diễn và các phim họ đã tham gia trên ZMovie.",
  });

  return {
    locale,
    query,
    people,
    total,
    pending,
    error,
    hasMore,
    load,
    loadMore,
  };
}
