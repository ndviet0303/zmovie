<script setup lang="ts">
import { ChevronRight, Flame, Info, Play } from "@lucide/vue";

const {
  home,
  error,
  activeLocale,
  messages,
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
} = await useHomePage();

const text = computed(() => ({
  ...messages.value.home,
  new: messages.value.home.newRelease,
  watch: messages.value.home.watchNow,
  empty: messages.value.home.emptyTrending,
}));
</script>

<template>
  <main class="min-h-screen overflow-x-hidden bg-background text-foreground">
    <AppNavbar :locale="activeLocale" @locale-change="changeLocale" />

    <section
      v-if="home"
      class="relative flex min-h-175 items-end px-5 pb-16 pt-28 lg:min-h-217.5 lg:px-12 lg:pb-20"
    >
      <img
        :src="home.hero.posterUrl"
        :alt="home.hero.title"
        class="absolute inset-0 size-full object-cover object-center opacity-70"
      />
      <div
        class="absolute inset-0 bg-[linear-gradient(90deg,#131313_0%,rgba(19,19,19,.88)_28%,rgba(19,19,19,.34)_62%,rgba(19,19,19,.74)_100%),linear-gradient(0deg,#131313_0%,transparent_53%)]"
      />

      <div class="relative mx-auto w-full max-w-360">
        <div class="max-w-2xl">
          <div class="mb-4 flex items-center gap-3 text-xs">
            <span
              class="rounded-full border border-white/10 bg-background/60 px-3 py-1.5 text-foreground backdrop-blur-sm"
              >{{ text.new }}</span
            >
            <span class="text-tertiary"
              >{{ home.hero.year }} · {{ home.hero.genre }} ·
              {{ text.movie }}</span
            >
          </div>
          <h1
            class="font-display max-w-xl text-5xl font-bold leading-[.98] tracking-[-.035em] text-foreground drop-shadow-md sm:text-6xl lg:text-7xl"
          >
            {{ home.hero.title }}
          </h1>
          <p
            class="mt-6 max-w-xl text-base leading-relaxed text-muted-foreground lg:text-lg"
          >
            {{ text.description }}
          </p>
          <div class="mt-8 flex flex-wrap items-center gap-4">
            <NuxtLink
              :to="`/watch/${home.hero.slug}`"
              class="inline-flex items-center gap-2 rounded-2xl bg-primary-container px-7 py-4 text-sm font-semibold text-primary-container-foreground transition hover:bg-primary"
            >
              <Play class="size-4 fill-current" /> {{ text.watch }}
            </NuxtLink>
            <NuxtLink
              :to="`/movies/${home.hero.slug}`"
              class="inline-flex items-center gap-2 rounded-2xl border border-white/15 bg-background/30 px-7 py-4 text-sm font-medium text-foreground backdrop-blur-sm transition hover:bg-surface-container"
            >
              <Info class="size-4" /> {{ text.details }}
            </NuxtLink>
          </div>
        </div>
      </div>
    </section>

    <section
      v-if="home"
      id="browse"
      class="mx-auto max-w-360 px-5 py-20 lg:px-12"
    >
      <div class="grid items-start gap-14 lg:grid-cols-[minmax(0,1fr)_19rem]">
        <div>
          <section v-if="continueWatching.length" class="mb-20">
            <div class="mb-8 flex items-end justify-between">
              <h2
                class="font-display text-3xl font-semibold tracking-tight lg:text-4xl"
              >
                {{ activeLocale === "vi" ? "Xem tiếp" : "Continue watching" }}
              </h2>
              <NuxtLink
                to="/my-list"
                class="inline-flex items-center gap-1 text-sm font-medium text-primary transition hover:text-primary-container"
                >{{ text.viewAll }} <ChevronRight class="size-4"
              /></NuxtLink>
            </div>
            <div class="grid grid-cols-2 gap-5 sm:grid-cols-3 xl:grid-cols-5">
              <NuxtLink
                v-for="item in continueWatching"
                :key="item.title.slug"
                :to="{
                  path: `/watch/${item.title.slug}`,
                  query: item.episodeNumber
                    ? { episode: item.episodeNumber }
                    : {},
                }"
                class="group relative aspect-video overflow-hidden rounded-2xl bg-surface-container"
              >
                <img
                  :src="item.title.posterUrl"
                  :alt="item.title.title"
                  class="absolute inset-0 size-full object-cover opacity-75 transition duration-300 group-hover:scale-105 group-hover:opacity-100"
                />
                <div
                  class="absolute inset-0 bg-gradient-to-t from-black/95 via-black/15 to-transparent"
                />
                <div class="absolute inset-x-0 bottom-0 p-3">
                  <h3 class="truncate text-sm font-semibold text-white">
                    {{ item.title.title }}
                  </h3>
                  <p
                    v-if="item.episodeNumber"
                    class="mt-0.5 text-[11px] text-white/65"
                  >
                    {{
                      activeLocale === "vi"
                        ? `Tập ${item.episodeNumber}`
                        : `Episode ${item.episodeNumber}`
                    }}
                  </p>
                  <div
                    class="mt-2 h-1 overflow-hidden rounded-full bg-white/25"
                  >
                    <span
                      class="block h-full rounded-full bg-primary"
                      :style="{ width: `${progressPercent(item)}%` }"
                    />
                  </div>
                </div>
              </NuxtLink>
            </div>
          </section>

          <section v-if="recommendedTitles.length">
            <div class="mb-8 flex items-end justify-between">
              <h2
                class="font-display text-3xl font-semibold tracking-tight lg:text-4xl"
              >
                {{ text.recommended }}
              </h2>
              <NuxtLink
                to="/browse?collection=recommended"
                class="inline-flex items-center gap-1 text-sm font-medium text-primary transition hover:text-primary-container"
                >{{ text.viewAll }} <ChevronRight class="size-4"
              /></NuxtLink>
            </div>
            <TitlePosterRow :titles="recommendedTitles" />
          </section>

          <section v-if="newReleaseTitles.length" class="mt-20">
            <div class="mb-8 flex items-end justify-between">
              <h2
                class="font-display text-3xl font-semibold tracking-tight lg:text-4xl"
              >
                {{ text.newReleases }}
              </h2>
              <NuxtLink
                to="/browse?sort=latest"
                class="inline-flex items-center gap-1 text-sm font-medium text-primary transition hover:text-primary-container"
                >{{ text.viewAll }} <ChevronRight class="size-4"
              /></NuxtLink>
            </div>
            <TitlePosterRow :titles="newReleaseTitles" />
          </section>

          <section v-if="titles2026.length" class="mt-20">
            <div class="mb-8 flex items-end justify-between">
              <h2
                class="font-display text-3xl font-semibold tracking-tight lg:text-4xl"
              >
                {{ text.year2026 }}
              </h2>
              <NuxtLink
                to="/browse?sort=latest"
                class="inline-flex items-center gap-1 text-sm font-medium text-primary transition hover:text-primary-container"
                >{{ text.viewAll }} <ChevronRight class="size-4"
              /></NuxtLink>
            </div>
            <TitlePosterRow :titles="titles2026" />
          </section>

          <section v-if="moviePicks.length" class="mt-20">
            <div class="mb-8 flex items-end justify-between">
              <h2
                class="font-display text-3xl font-semibold tracking-tight lg:text-4xl"
              >
                {{ text.moviePicks }}
              </h2>
              <NuxtLink
                to="/browse?sort=latest"
                class="inline-flex items-center gap-1 text-sm font-medium text-primary transition hover:text-primary-container"
                >{{ text.viewAll }} <ChevronRight class="size-4"
              /></NuxtLink>
            </div>
            <TitlePosterRow :titles="moviePicks" />
          </section>

          <section v-if="seriesPicks.length" class="mt-20">
            <div class="mb-8 flex items-end justify-between">
              <h2
                class="font-display text-3xl font-semibold tracking-tight lg:text-4xl"
              >
                {{ text.seriesPicks }}
              </h2>
              <NuxtLink
                to="/browse?type=series"
                class="inline-flex items-center gap-1 text-sm font-medium text-primary transition hover:text-primary-container"
                >{{ text.viewAll }} <ChevronRight class="size-4"
              /></NuxtLink>
            </div>
            <TitlePosterRow :titles="seriesPicks" />
          </section>
        </div>

        <aside class="lg:sticky lg:top-24">
          <div
            class="rounded-3xl border border-white/10 bg-surface-container p-5 shadow-[inset_0_1px_0_rgba(235,225,214,.08)]"
          >
            <p
              class="inline-flex items-center gap-2 text-xs font-semibold uppercase tracking-[.16em] text-primary"
            >
              <Flame class="size-4 fill-current" /> {{ text.trending }}
            </p>
            <div
              class="mt-4 flex rounded-full border border-white/10 bg-background/50 p-1"
              role="tablist"
              :aria-label="text.trending"
            >
              <button
                v-for="period in topPeriods"
                :key="period"
                class="flex-1 rounded-full px-2 py-2 text-[11px] font-semibold transition"
                :class="
                  topPeriod === period
                    ? 'bg-primary-container text-primary-container-foreground shadow-sm'
                    : 'text-muted-foreground hover:text-foreground'
                "
                :aria-selected="topPeriod === period"
                role="tab"
                @click="selectTopPeriod(period)"
              >
                {{ text.periods[period] }}
              </button>
            </div>

            <div v-if="topTitles?.length" class="mt-5 divide-y divide-white/8">
              <NuxtLink
                v-for="(item, index) in topTitles.slice(0, 5)"
                :key="item.title.slug"
                :to="`/movies/${item.title.slug}`"
                class="group flex gap-3 py-3 first:pt-0 last:pb-0"
              >
                <span
                  class="flex w-5 shrink-0 items-center justify-center font-display text-xl font-semibold text-primary"
                  >{{ index + 1 }}</span
                >
                <img
                  :src="item.title.posterUrl"
                  :alt="item.title.title"
                  class="h-16 w-11 rounded-lg object-cover"
                  loading="lazy"
                />
                <div class="min-w-0 py-0.5">
                  <h3
                    class="truncate text-sm font-semibold text-foreground transition group-hover:text-primary"
                  >
                    {{ item.title.title }}
                  </h3>
                  <p class="mt-1 truncate text-xs text-muted-foreground">
                    {{ item.title.genre }}
                  </p>
                  <p class="mt-1 text-[11px] font-medium text-primary">
                    {{ formatViews(item.views) }} {{ text.views }}
                  </p>
                </div>
              </NuxtLink>
            </div>
            <p
              v-else-if="!topPending"
              class="mt-5 text-sm leading-6 text-muted-foreground"
            >
              {{ text.empty }}
            </p>
          </div>
        </aside>
      </div>
    </section>

    <section
      v-if="error"
      class="mx-auto max-w-360 px-5 pb-12 text-sm text-destructive lg:px-12"
    >
      {{ text.unavailable }}
    </section>

    <footer
      class="border-t border-white/5 bg-surface-container-lowest px-5 py-10 lg:px-12"
    >
      <div
        class="mx-auto flex max-w-360 flex-col items-center gap-5 text-center"
      >
        <NuxtLink to="/" class="font-display text-xl font-semibold text-primary"
          >ZMovie</NuxtLink
        >
        <nav
          class="flex flex-wrap justify-center gap-x-6 gap-y-2 text-xs text-muted-foreground"
        >
          <a href="#">Privacy Policy</a><a href="#">Terms of Service</a
          ><a href="#">Help Center</a><a href="#">Contact Us</a>
        </nav>
        <p class="text-xs text-tertiary">
          © 2026 ZMovie Premium. All rights reserved.
        </p>
      </div>
    </footer>
  </main>
</template>
