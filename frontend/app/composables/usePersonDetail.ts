import { computed } from "vue";
import { fetchCatalogPerson } from "~/services/catalog.service";

export async function usePersonDetail() {
  const route = useRoute();
  const { locale } = useLocale();
  const slug = computed(() => String(route.params.slug ?? ""));
  const {
    data: person,
    pending,
    error,
    refresh,
  } = await useAsyncData(
    () => `person-${slug.value}-${locale.value}`,
    () => fetchCatalogPerson(slug.value, locale.value),
  );

  useZMovieSeo({
    title: computed(() => person.value?.name ?? "Nghệ sĩ"),
    description: computed(() =>
      person.value
        ? `Khám phá các phim có sự tham gia của ${person.value.name} trên ZMovie.`
        : "Hồ sơ nghệ sĩ trên ZMovie.",
    ),
    image: computed(() => person.value?.titles[0]?.posterUrl),
  });

  return { locale, person, pending, error, refresh };
}
