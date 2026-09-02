import { computed, toValue } from "vue";
import type { MaybeRefOrGetter } from "vue";

export type SeoOptions = {
  title: MaybeRefOrGetter<string>;
  description: MaybeRefOrGetter<string>;
  image?: MaybeRefOrGetter<string | null | undefined>;
  type?: MaybeRefOrGetter<string>;
  year?: MaybeRefOrGetter<number | undefined>;
  director?: MaybeRefOrGetter<string | undefined>;
  actors?: MaybeRefOrGetter<string | undefined>;
  genre?: MaybeRefOrGetter<string | undefined>;
  trailerUrl?: MaybeRefOrGetter<string | undefined>;
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

  const schemaJsonLd = computed(() => {
    const mediaType = toValue(options.type);
    if (
      mediaType !== "video.movie" &&
      mediaType !== "movie" &&
      mediaType !== "series"
    ) {
      return null;
    }

    const actorsStr = toValue(options.actors) || "";
    const actorsList = actorsStr
      ? actorsStr.split(",").map((a) => ({ "@type": "Person", name: a.trim() }))
      : [];

    const directorName = toValue(options.director) || "";
    const trailer = toValue(options.trailerUrl);

    const jsonLd: Record<string, unknown> = {
      "@context": "https://schema.org",
      "@type": mediaType === "series" ? "TVSeries" : "Movie",
      name: toValue(options.title),
      description: description.value,
      image: image.value,
      url: url.value,
    };

    if (toValue(options.genre)) jsonLd.genre = toValue(options.genre);
    if (toValue(options.year))
      jsonLd.datePublished = String(toValue(options.year));
    if (directorName)
      jsonLd.director = { "@type": "Person", name: directorName };
    if (actorsList.length > 0) jsonLd.actor = actorsList;
    if (trailer) {
      jsonLd.trailer = {
        "@type": "VideoObject",
        name: `${toValue(options.title)} - Official Trailer`,
        embedUrl: trailer,
        thumbnailUrl: image.value,
        description: description.value,
      };
    }

    return jsonLd;
  });

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
    script: schemaJsonLd.value
      ? [
          {
            type: "application/ld+json",
            innerHTML: JSON.stringify(schemaJsonLd.value),
          },
        ]
      : [],
  }));
}
