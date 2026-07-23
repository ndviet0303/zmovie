<script setup lang="ts">
import { Bookmark, Check, Clock3, Info, Play, Share2, Star } from "@lucide/vue";

type TitleDetail = {
  slug: string;
  title: string;
  synopsis: string;
  genre: string;
  year: number;
  type: string;
  posterUrl: string;
  runtimeMinutes: number;
};
type TitleSummary = Pick<
  TitleDetail,
  "slug" | "title" | "genre" | "year" | "type" | "posterUrl"
>;
type TitleListResponse = { items: TitleSummary[] };
type Review = {
  id: string;
  authorName: string;
  rating: number;
  comment: string | null;
  updatedAt: string;
};
type ReviewsResponse = {
  averageRating: number;
  ratingCount: number;
  items: Review[];
};

const route = useRoute();
const locale = useCookie<"vi" | "en">("zmovie-locale", { default: () => "vi" });
const { $api } = useNuxtApp();
const slug = computed(() => String(route.params.slug));
const { data: title, error } = await useAsyncData(
  () => `movie-${slug.value}-${locale.value}`,
  () =>
    $api<TitleDetail>(`/v1/catalog/titles/${slug.value}`, {
      query: { locale: locale.value },
    }),
);
const { data: catalog } = await useAsyncData(
  () => `movie-recommendations-${locale.value}`,
  () =>
    $api<TitleListResponse>("/v1/catalog/titles", {
      query: { locale: locale.value },
    }),
);
const { data: reviews, refresh: refreshReviews } = await useAsyncData(
  () => `movie-reviews-${slug.value}`,
  () => $api<ReviewsResponse>(`/v1/catalog/titles/${slug.value}/reviews`),
);
const isSaved = ref(false);
const actionNotice = ref("");
const reviewRating = ref(0);
const reviewComment = ref("");
const isSubmittingReview = ref(false);
const copy = computed(() =>
  locale.value === "vi"
    ? {
        watch: "Xem ngay",
        trailer: "Trailer",
        saved: "Đã lưu",
        save: "Lưu phim",
        info: "Thông tin chi tiết",
        cast: "Dàn diễn viên & ê-kíp",
        related: "Có thể bạn sẽ thích",
        minutes: "phút",
        movie: "Phim lẻ",
        series: "Phim bộ",
        director: "Đạo diễn",
        country: "Quốc gia",
        language: "Ngôn ngữ",
        vietnam: "Việt Nam",
        vietnamese: "Tiếng Việt",
        savedNotice: "Đã thêm vào danh sách.",
        removedNotice: "Đã bỏ khỏi danh sách.",
        signIn: "Hãy đăng nhập để lưu phim.",
        copied: "Đã sao chép liên kết.",
        reviews: "Đánh giá & bình luận",
        yourRating: "Điểm của bạn",
        commentPlaceholder: "Chia sẻ cảm nhận về bộ phim này…",
        publish: "Đăng đánh giá",
        noReviews: "Chưa có đánh giá nào. Hãy là người đầu tiên!",
        submitted: "Đã lưu đánh giá của bạn.",
      }
    : {
        watch: "Watch now",
        trailer: "Trailer",
        saved: "Saved",
        save: "Save title",
        info: "Title information",
        cast: "Cast & crew",
        related: "You may also like",
        minutes: "min",
        movie: "Movie",
        series: "Series",
        director: "Director",
        country: "Country",
        language: "Language",
        vietnam: "Vietnam",
        vietnamese: "Vietnamese",
        savedNotice: "Added to your list.",
        removedNotice: "Removed from your list.",
        signIn: "Sign in to save titles.",
        copied: "Link copied.",
        reviews: "Ratings & reviews",
        yourRating: "Your rating",
        commentPlaceholder: "Share what you think about this title…",
        publish: "Publish review",
        noReviews: "No reviews yet. Be the first!",
        submitted: "Your review has been saved.",
      },
);
useZMovieSeo({
  title: computed(() => title.value?.title ?? "Chi tiết phim"),
  description: computed(
    () =>
      title.value?.synopsis ??
      "Xem thông tin, trailer và đánh giá phim trên ZMovie.",
  ),
  image: computed(() => title.value?.posterUrl),
  type: "video.movie",
});

const related = computed(() =>
  (catalog.value?.items ?? [])
    .filter((item) => item.slug !== title.value?.slug)
    .slice(0, 5),
);

async function setLocale(nextLocale: "vi" | "en") {
  if (nextLocale === locale.value) return;
  locale.value = nextLocale;
  await refreshNuxtData([
    `movie-${slug.value}-${nextLocale}`,
    `movie-recommendations-${nextLocale}`,
  ]);
}

async function loadSavedState() {
  try {
    const library = await $api<{ saved: { slug: string }[] }>(
      "/v1/me/library",
      { credentials: "include", query: { locale: locale.value } },
    );
    isSaved.value = library.saved.some(
      (item) => item.slug === title.value?.slug,
    );
  } catch {
    isSaved.value = false;
  }
}

async function toggleSaved() {
  if (!title.value) return;
  try {
    await $api(`/v1/me/saved/${title.value.slug}`, {
      method: isSaved.value ? "DELETE" : "PUT",
      credentials: "include",
    });
    isSaved.value = !isSaved.value;
    actionNotice.value = isSaved.value
      ? copy.value.savedNotice
      : copy.value.removedNotice;
  } catch {
    actionNotice.value = copy.value.signIn;
  }
}

function openTrailer() {
  if (!title.value) return;
  window.open(
    `https://www.youtube.com/results?search_query=${encodeURIComponent(`${title.value.title} trailer`)}`,
    "_blank",
    "noopener,noreferrer",
  );
}

async function shareTitle() {
  if (!title.value) return;
  try {
    if (navigator.share)
      await navigator.share({
        title: title.value.title,
        url: window.location.href,
      });
    else {
      await navigator.clipboard.writeText(window.location.href);
      actionNotice.value = copy.value.copied;
    }
  } catch {
    // Clipboard/share failures do not block the movie details page.
  }
}

async function submitReview() {
  if (!title.value || reviewRating.value < 1 || isSubmittingReview.value)
    return;
  isSubmittingReview.value = true;
  try {
    await $api(`/v1/me/titles/${title.value.slug}/review`, {
      method: "PUT",
      credentials: "include",
      body: {
        rating: reviewRating.value,
        comment: reviewComment.value || null,
      },
    });
    reviewComment.value = "";
    actionNotice.value = copy.value.submitted;
    await refreshReviews();
  } catch {
    actionNotice.value = copy.value.signIn;
  } finally {
    isSubmittingReview.value = false;
  }
}

function formattedDate(value: string) {
  return new Intl.DateTimeFormat(locale.value === "vi" ? "vi-VN" : "en-US", {
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(new Date(value));
}

onMounted(() => {
  void loadSavedState();
});
</script>

<template>
  <main class="min-h-screen bg-background text-foreground">
    <AppNavbar :locale="locale" @locale-change="setLocale" />
    <template v-if="title">
      <section
        class="relative overflow-hidden border-b border-white/5 px-5 py-14 lg:px-12 lg:py-20"
      >
        <img
          :src="title.posterUrl"
          :alt="title.title"
          class="absolute inset-0 size-full object-cover opacity-25 blur-sm"
        />
        <div
          class="absolute inset-0 bg-[linear-gradient(90deg,#131313_0%,rgba(19,19,19,.9)_38%,rgba(19,19,19,.75)_100%),linear-gradient(0deg,#131313_0%,transparent_75%)]"
        />
        <div
          class="relative mx-auto grid max-w-360 items-end gap-10 md:grid-cols-[240px_1fr] lg:grid-cols-[280px_1fr]"
        >
          <img
            :src="title.posterUrl"
            :alt="title.title"
            class="mx-auto aspect-[2/3] w-48 rounded-3xl object-cover shadow-[0_18px_50px_rgba(0,0,0,.5)] md:mx-0 md:w-full"
          />
          <div class="max-w-3xl pb-2">
            <div class="mb-4 flex flex-wrap items-center gap-2 text-xs">
              <span
                class="rounded-md bg-primary px-2 py-1 font-bold text-primary-container-foreground"
                >4K</span
              ><span
                class="rounded-md border border-white/15 bg-background/50 px-2 py-1"
                >{{ title.type === "movie" ? copy.movie : copy.series }}</span
              ><span class="text-tertiary"
                >{{ title.year }} · {{ title.genre }} ·
                {{ title.runtimeMinutes }} {{ copy.minutes }}</span
              >
            </div>
            <h1
              class="font-display text-4xl font-semibold tracking-tight sm:text-5xl lg:text-6xl"
            >
              {{ title.title }}
            </h1>
            <p
              class="mt-5 max-w-2xl text-base leading-relaxed text-muted-foreground"
            >
              {{ title.synopsis }}
            </p>
            <div class="mt-8 flex flex-wrap gap-3">
              <NuxtLink
                :to="`/watch/${title.slug}`"
                class="inline-flex items-center gap-2 rounded-2xl bg-primary-container px-6 py-3.5 text-sm font-semibold text-primary-container-foreground"
                ><Play class="size-4 fill-current" />{{ copy.watch }}</NuxtLink
              ><button
                class="inline-flex items-center gap-2 rounded-2xl border border-white/15 bg-background/30 px-6 py-3.5 text-sm font-medium transition hover:border-primary/60"
                @click="openTrailer"
              >
                <Info class="size-4" />{{ copy.trailer }}</button
              ><button
                class="grid size-12 place-items-center rounded-2xl border border-white/15 bg-background/30 transition hover:border-primary/60"
                :aria-label="copy.save"
                @click="toggleSaved"
              >
                <Check v-if="isSaved" class="size-5 text-primary" /><Bookmark
                  v-else
                  class="size-5"
                /></button
              ><button
                class="grid size-12 place-items-center rounded-2xl border border-white/15 bg-background/30 transition hover:border-primary/60"
                aria-label="Share"
                @click="shareTitle"
              >
                <Share2 class="size-5" />
              </button>
            </div>
            <p
              v-if="actionNotice"
              class="mt-3 text-xs font-medium text-primary"
              role="status"
            >
              {{ actionNotice }}
            </p>
          </div>
        </div>
      </section>

      <section
        class="mx-auto grid max-w-360 gap-6 px-5 py-14 lg:grid-cols-[1.6fr_.8fr] lg:px-12"
      >
        <article
          class="rounded-3xl border border-white/10 bg-surface-container p-7"
        >
          <h2 class="font-display text-2xl font-semibold">{{ copy.cast }}</h2>
          <p class="mt-2 text-sm text-tertiary">ZMovie Originals</p>
          <div class="mt-6 flex flex-wrap gap-5">
            <div
              v-for="person in ['Linh Phạm', 'Minh Anh', 'Đức Thành', 'Hà My']"
              :key="person"
              class="text-center"
            >
              <span
                class="mx-auto grid size-12 place-items-center rounded-full bg-surface-container-lowest text-primary"
                ><Star class="size-4 fill-current"
              /></span>
              <p class="mt-2 text-xs text-foreground">{{ person }}</p>
              <p class="text-[11px] text-muted-foreground">
                {{ copy.director }}
              </p>
            </div>
          </div>
        </article>
        <aside
          class="rounded-3xl border border-white/10 bg-surface-container p-7"
        >
          <h2 class="font-display text-2xl font-semibold">{{ copy.info }}</h2>
          <dl class="mt-5 space-y-4 text-sm">
            <div class="flex justify-between gap-4">
              <dt class="text-muted-foreground">{{ copy.country }}</dt>
              <dd>{{ copy.vietnam }}</dd>
            </div>
            <div class="flex justify-between gap-4">
              <dt class="text-muted-foreground">{{ copy.language }}</dt>
              <dd>{{ copy.vietnamese }}</dd>
            </div>
            <div class="flex justify-between gap-4">
              <dt class="text-muted-foreground">ZMovie</dt>
              <dd class="inline-flex items-center gap-1 text-primary">
                <Star class="size-3 fill-current" />{{
                  reviews?.ratingCount ? reviews.averageRating.toFixed(1) : "—"
                }}
              </dd>
            </div>
            <div class="flex justify-between gap-4">
              <dt class="text-muted-foreground">
                <Clock3 class="inline size-3" /> Runtime
              </dt>
              <dd>{{ title.runtimeMinutes }} {{ copy.minutes }}</dd>
            </div>
          </dl>
        </aside>
      </section>

      <section
        class="mx-auto grid max-w-360 gap-6 px-5 pb-14 lg:grid-cols-[.8fr_1.2fr] lg:px-12"
      >
        <form
          class="rounded-3xl border border-white/10 bg-surface-container p-6"
          @submit.prevent="submitReview"
        >
          <h2 class="font-display text-2xl font-semibold">
            {{ copy.reviews }}
          </h2>
          <p class="mt-2 text-sm text-muted-foreground">
            {{ copy.yourRating }}
            <span class="font-semibold text-primary"
              >{{ reviewRating || "—" }}/10</span
            >
          </p>
          <div class="mt-4 flex flex-wrap gap-1">
            <button
              v-for="score in 10"
              :key="score"
              type="button"
              class="grid size-8 place-items-center rounded-lg text-xs font-bold transition"
              :class="
                score <= reviewRating
                  ? 'bg-primary-container text-primary-container-foreground'
                  : 'bg-surface-container-high text-muted-foreground hover:text-primary'
              "
              @click="reviewRating = score"
            >
              {{ score }}
            </button>
          </div>
          <textarea
            v-model="reviewComment"
            class="mt-5 min-h-28 w-full resize-y rounded-2xl border border-white/10 bg-background p-4 text-sm outline-none transition placeholder:text-muted-foreground focus:border-primary"
            :placeholder="copy.commentPlaceholder"
            maxlength="2000"
          />
          <button
            class="mt-4 rounded-2xl bg-primary-container px-5 py-3 text-sm font-semibold text-primary-container-foreground transition hover:bg-primary disabled:opacity-50"
            :disabled="reviewRating < 1 || isSubmittingReview"
          >
            {{ copy.publish }}
          </button>
        </form>
        <div
          class="rounded-3xl border border-white/10 bg-surface-container p-6"
        >
          <div class="flex items-baseline justify-between gap-4">
            <h2 class="font-display text-2xl font-semibold">
              {{ reviews?.averageRating.toFixed(1) ?? "—" }}
              <span class="text-base text-muted-foreground">/ 10</span>
            </h2>
            <p class="text-sm text-muted-foreground">
              {{ reviews?.ratingCount ?? 0 }} {{ copy.reviews }}
            </p>
          </div>
          <div
            v-if="reviews?.items.length"
            class="mt-5 divide-y divide-white/8"
          >
            <article
              v-for="review in reviews.items"
              :key="review.id"
              class="py-4 first:pt-0 last:pb-0"
            >
              <div class="flex items-center justify-between gap-4">
                <p class="font-semibold">{{ review.authorName }}</p>
                <span
                  class="inline-flex items-center gap-1 text-sm font-semibold text-primary"
                  ><Star class="size-3 fill-current" />{{
                    review.rating
                  }}/10</span
                >
              </div>
              <p
                v-if="review.comment"
                class="mt-2 text-sm leading-6 text-muted-foreground"
              >
                {{ review.comment }}
              </p>
              <p class="mt-2 text-xs text-tertiary">
                {{ formattedDate(review.updatedAt) }}
              </p>
            </article>
          </div>
          <p v-else class="mt-5 text-sm text-muted-foreground">
            {{ copy.noReviews }}
          </p>
        </div>
      </section>

      <section
        v-if="related.length"
        class="mx-auto max-w-360 px-5 pb-20 lg:px-12"
      >
        <h2 class="font-display text-3xl font-semibold">{{ copy.related }}</h2>
        <div class="mt-7 grid grid-cols-2 gap-5 sm:grid-cols-3 md:grid-cols-5">
          <NuxtLink
            v-for="item in related"
            :key="item.slug"
            :to="`/movies/${item.slug}`"
            class="group relative aspect-[2/3] overflow-hidden rounded-3xl bg-surface-container"
            ><img
              :src="item.posterUrl"
              :alt="item.title"
              class="size-full object-cover opacity-80 transition group-hover:scale-105 group-hover:opacity-100"
            />
            <div
              class="absolute inset-x-0 bottom-0 bg-gradient-to-t from-black/90 to-transparent p-4 pt-12"
            >
              <h3 class="font-display truncate text-lg">{{ item.title }}</h3>
              <p class="text-xs text-tertiary">
                {{ item.year }} · {{ item.genre }}
              </p>
            </div></NuxtLink
          >
        </div>
      </section>
    </template>
    <p
      v-else
      class="mx-auto max-w-360 px-5 py-24 text-center text-muted-foreground"
    >
      {{ error ? "Title not found." : "Loading…" }}
    </p>
  </main>
</template>
