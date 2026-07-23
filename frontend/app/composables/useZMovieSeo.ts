import { computed, toValue } from "vue";
import type { MaybeRefOrGetter } from "vue";

type SeoOptions = {
  title: MaybeRefOrGetter<string>;
  description: MaybeRefOrGetter<string>;
  image?: MaybeRefOrGetter<string | null | undefined>;
  type?: MaybeRefOrGetter<string>;
};

function toAbsoluteUrl(value: string | null | undefined, siteUrl: string) {
  if (!value) return `${siteUrl}/og-image.svg`;
  try {
    return new URL(value, siteUrl).href;
  } catch {
    return `${siteUrl}/og-image.svg`;
  }
}

export function useZMovieSeo(options: SeoOptions) {
  const route = useRoute();
  const config = useRuntimeConfig();
  const siteUrl = String(
    config.public.siteUrl || "https://movie.ziet.dev",
  ).replace(/\/$/, "");
  const title = computed(() => {
    const value = toValue(options.title).trim();
    return value.includes("ZMovie") ? value : `${value} — ZMovie`;
  });
  const description = computed(() => toValue(options.description).trim());
  const image = computed(() => toAbsoluteUrl(toValue(options.image), siteUrl));
  const url = computed(() => new URL(route.fullPath, siteUrl).href);
  const type = computed(() => toValue(options.type) || "website");

  useHead(() => ({
    title: title.value,
    link: [{ rel: "canonical", href: url.value }],
    meta: [
      { name: "description", content: description.value },
      { property: "og:title", content: title.value },
      { property: "og:description", content: description.value },
      { property: "og:image", content: image.value },
      { property: "og:image:alt", content: title.value },
      { property: "og:url", content: url.value },
      { property: "og:type", content: type.value },
      { name: "twitter:title", content: title.value },
      { name: "twitter:description", content: description.value },
      { name: "twitter:image", content: image.value },
      { name: "twitter:image:alt", content: title.value },
    ],
  }));
}
