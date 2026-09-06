import { computed, ref, watch } from "vue";
import { fetchCatalogSchedule } from "~/services/catalog.service";
import type { ScheduleEntry } from "~/types/catalog";

function mondayOf(date: Date): Date {
  const value = new Date(date);
  const daysSinceMonday = (value.getUTCDay() + 6) % 7;
  value.setUTCDate(value.getUTCDate() - daysSinceMonday);
  value.setUTCHours(0, 0, 0, 0);
  return value;
}

function toDateValue(date: Date): string {
  return date.toISOString().slice(0, 10);
}

export async function useSchedule() {
  const route = useRoute();
  const router = useRouter();
  const { locale } = useLocale();
  const requestedWeek = Array.isArray(route.query.week)
    ? route.query.week[0]
    : route.query.week;
  const parsedWeek = requestedWeek
    ? new Date(`${requestedWeek}T00:00:00Z`)
    : new Date();
  const weekStart = ref(
    toDateValue(
      mondayOf(Number.isNaN(parsedWeek.getTime()) ? new Date() : parsedWeek),
    ),
  );

  const { data, pending, error, refresh } = await useAsyncData(
    "catalog-schedule",
    () =>
      fetchCatalogSchedule({
        weekStart: weekStart.value,
        locale: locale.value,
      }),
  );

  const days = computed(() => {
    const start = new Date(`${weekStart.value}T00:00:00Z`);
    return Array.from({ length: 7 }, (_, offset) => {
      const date = new Date(start);
      date.setUTCDate(start.getUTCDate() + offset);
      const value = toDateValue(date);
      return {
        value,
        date,
        items: (data.value?.items ?? []).filter(
          (item: ScheduleEntry) => item.date === value,
        ),
      };
    });
  });

  const weekLabel = computed(() => {
    const first = days.value[0]?.date;
    const last = days.value[6]?.date;
    if (!first || !last) return "";
    return `${first.toLocaleDateString("vi-VN")} – ${last.toLocaleDateString("vi-VN")}`;
  });

  async function changeWeek(offset: number) {
    const date = new Date(`${weekStart.value}T00:00:00Z`);
    date.setUTCDate(date.getUTCDate() + offset * 7);
    weekStart.value = toDateValue(date);
  }

  async function goToCurrentWeek() {
    weekStart.value = toDateValue(mondayOf(new Date()));
  }

  watch([weekStart, locale], async () => {
    await router.replace({ query: { ...route.query, week: weekStart.value } });
    await refresh();
  });

  return {
    locale,
    days,
    weekLabel,
    pending,
    error,
    changeWeek,
    goToCurrentWeek,
    refresh,
  };
}
