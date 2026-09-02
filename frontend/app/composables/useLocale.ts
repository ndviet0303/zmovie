import { computed } from "vue";
import type { Messages, SupportedLocale } from "~/i18n/types";
import { vi } from "~/i18n/vi";
import { en } from "~/i18n/en";

const dictionaries: Record<SupportedLocale, Messages> = { vi, en };

export function useLocale() {
  const locale = useCookie<SupportedLocale>("zmovie-locale", {
    default: () => "vi",
  });

  const messages = computed<Messages>(() => dictionaries[locale.value] ?? vi);
  const isVietnamese = computed(() => locale.value === "vi");

  function setLocale(nextLocale: SupportedLocale) {
    locale.value = nextLocale;
  }

  return {
    locale,
    messages,
    isVietnamese,
    setLocale,
  };
}
