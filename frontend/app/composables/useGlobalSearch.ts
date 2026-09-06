import { onBeforeUnmount, ref, watch } from "vue";
import { searchCatalogTitles } from "~/services/search.service";
import type { TitleSummary } from "~/types/catalog";

export function useGlobalSearch() {
  const router = useRouter();
  const { locale } = useLocale();
  const query = ref("");
  const results = ref<TitleSummary[]>([]);
  const isOpen = ref(false);
  const isLoading = ref(false);
  let searchTimer: number | undefined;
  let requestSequence = 0;

  watch(query, (value) => {
    clearTimeout(searchTimer);
    const normalized = value.trim();
    if (normalized.length < 2) {
      results.value = [];
      isLoading.value = false;
      return;
    }

    const sequence = ++requestSequence;
    isLoading.value = true;
    searchTimer = window.setTimeout(async () => {
      try {
        const response = await searchCatalogTitles({
          q: normalized,
          locale: locale.value,
        });
        if (sequence === requestSequence) {
          results.value = response.items.slice(0, 6);
        }
      } catch {
        if (sequence === requestSequence) results.value = [];
      } finally {
        if (sequence === requestSequence) isLoading.value = false;
      }
    }, 220);
  });

  function open() {
    isOpen.value = true;
  }

  function close() {
    isOpen.value = false;
  }

  function blur() {
    window.setTimeout(close, 150);
  }

  async function submit() {
    const normalized = query.value.trim();
    if (!normalized) return;
    close();
    await router.push({ path: "/browse", query: { query: normalized } });
  }

  function select(slug: string) {
    close();
    void router.push(`/movies/${slug}`);
  }

  onBeforeUnmount(() => clearTimeout(searchTimer));

  return {
    query,
    results,
    isOpen,
    isLoading,
    open,
    close,
    blur,
    submit,
    select,
  };
}
