<script setup lang="ts">
import { Check, ChevronDown, Search, SlidersHorizontal, X } from "@lucide/vue";
import { InputField } from "~/components/ui/input";

const {
  locale,
  query,
  selectedGenre,
  filtersOpen,
  sortOrder,
  isRecommended,
  isLoading,
  loadError,
  genres,
  visibleTitles,
  activeFilterCount,
  copy,
  genreLabel,
  clearFilters,
  changeLocale,
} = useBrowse();
</script>

<template>
  <main class="min-h-screen bg-background text-foreground">
    <AppNavbar :locale="locale" @locale-change="changeLocale" />

    <section class="mx-auto max-w-360 px-5 pb-24 pt-12 lg:px-12 lg:pt-16">
      <h1 class="font-display text-4xl font-bold tracking-tight sm:text-5xl">
        {{ copy.title }}
      </h1>
      <InputField
        v-model="query"
        :placeholder="copy.placeholder"
        class="mt-7 max-w-2xl"
      >
        <template #leading>
          <Search class="size-5 shrink-0 text-muted-foreground" />
        </template>
        <template #trailing>
          <button
            v-if="query"
            class="shrink-0 text-muted-foreground transition-colors hover:text-primary"
            aria-label="Clear search"
            @click="query = ''"
          >
            <X class="size-5" />
          </button>
        </template>
      </InputField>

      <div id="filters" class="mt-8 flex flex-wrap items-center gap-3">
        <button
          class="inline-flex items-center gap-2 rounded-xl border border-white/10 bg-surface-container px-4 py-3 text-xs font-semibold text-foreground transition hover:border-primary/60"
          @click="filtersOpen = true"
        >
          <SlidersHorizontal class="size-4 text-primary" />
          {{ copy.filters }}
          <span
            v-if="activeFilterCount"
            class="inline-flex size-5 items-center justify-center rounded-full bg-primary text-[10px] text-primary-container-foreground"
          >
            {{ activeFilterCount }}
          </span>
        </button>
        <button
          v-if="selectedGenre !== 'all'"
          class="inline-flex items-center gap-2 rounded-full border border-primary/40 bg-primary/10 px-3 py-2 text-xs text-primary"
          @click="clearFilters"
        >
          {{ genreLabel(selectedGenre) }}
          <X class="size-3.5" />
        </button>
        <span class="text-xs text-muted-foreground">
          {{ visibleTitles.length }} {{ copy.titlesCount }}
        </span>
        <label
          v-if="!isRecommended"
          class="ml-auto inline-flex items-center gap-2 rounded-xl border border-white/10 bg-surface-container px-3 text-xs text-muted-foreground"
        >
          <span class="sr-only">{{ copy.latest }}</span>
          <select
            v-model="sortOrder"
            class="h-10 cursor-pointer appearance-none bg-transparent pr-5 text-xs text-foreground outline-none"
          >
            <option value="latest">{{ copy.latest }}</option>
            <option value="oldest">{{ copy.oldest }}</option>
            <option value="title">{{ copy.titleAZ }}</option>
          </select>
          <ChevronDown class="pointer-events-none -ml-5 size-4" />
        </label>
      </div>

      <div
        v-if="filtersOpen"
        class="fixed inset-0 z-50 flex items-end justify-center bg-black/70 p-0 backdrop-blur-sm sm:items-center sm:p-5"
        @click.self="filtersOpen = false"
      >
        <section
          class="max-h-[85vh] w-full overflow-hidden rounded-t-3xl border border-white/10 bg-surface-container-lowest shadow-2xl sm:max-w-2xl sm:rounded-3xl"
          role="dialog"
          aria-modal="true"
          aria-labelledby="filter-title"
        >
          <div
            class="flex items-center justify-between border-b border-white/10 px-5 py-4 sm:px-6"
          >
            <div>
              <h2 id="filter-title" class="font-display text-xl font-semibold">
                {{ copy.filters }}
              </h2>
              <p class="mt-1 text-xs text-muted-foreground">
                {{ copy.chooseGenre }}
              </p>
            </div>
            <button
              class="rounded-full p-2 text-muted-foreground transition hover:bg-white/10 hover:text-foreground"
              aria-label="Close filters"
              @click="filtersOpen = false"
            >
              <X class="size-5" />
            </button>
          </div>

          <div class="max-h-[55vh] overflow-y-auto p-5 sm:p-6">
            <div class="grid grid-cols-2 gap-2 sm:grid-cols-3">
              <button
                v-for="genre in genres"
                :key="genre"
                class="flex min-h-11 items-center justify-between rounded-xl border px-3 py-2 text-left text-xs transition"
                :class="
                  selectedGenre === genre
                    ? 'border-primary/60 bg-primary/15 text-primary'
                    : 'border-white/10 bg-surface-container text-muted-foreground hover:border-primary/40 hover:text-foreground'
                "
                @click="selectedGenre = genre"
              >
                <span>{{ genreLabel(genre) }}</span>
                <Check
                  v-if="selectedGenre === genre"
                  class="ml-2 size-4 shrink-0"
                />
              </button>
            </div>
          </div>

          <div
            class="flex items-center justify-between border-t border-white/10 px-5 py-4 sm:px-6"
          >
            <button
              class="text-xs font-medium text-muted-foreground transition hover:text-foreground"
              @click="clearFilters"
            >
              {{ copy.clearFilters }}
            </button>
            <button
              class="rounded-xl bg-primary px-5 py-3 text-xs font-semibold text-primary-container-foreground transition hover:opacity-90"
              @click="filtersOpen = false"
            >
              {{ copy.showResults }}
            </button>
          </div>
        </section>
      </div>

      <p
        v-if="isLoading"
        class="mt-10 rounded-3xl border border-white/10 bg-surface-container p-10 text-center text-muted-foreground"
      >
        {{ copy.loading }}
      </p>
      <p
        v-else-if="loadError"
        class="mt-10 rounded-3xl border border-white/10 bg-surface-container p-10 text-center text-muted-foreground"
      >
        {{ copy.error }}
      </p>
      <div
        v-else-if="visibleTitles.length"
        class="mt-10 grid grid-cols-2 gap-5 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5"
      >
        <NuxtLink
          v-for="(title, index) in visibleTitles"
          :key="title.slug"
          :to="`/movies/${title.slug}`"
          class="group relative aspect-[2/3] overflow-hidden rounded-3xl bg-surface-container shadow-[inset_0_1px_0_rgba(235,225,214,.1)] transition duration-300 hover:scale-[1.02] hover:shadow-[0_16px_40px_rgba(217,131,103,.16)]"
        >
          <img
            :src="title.posterUrl"
            :alt="title.title"
            class="absolute inset-0 size-full object-cover opacity-80 transition duration-500 group-hover:opacity-100"
            loading="lazy"
          />
          <div
            class="absolute inset-0 bg-gradient-to-t from-black/95 via-black/20 to-transparent"
          />
          <span
            class="absolute left-4 top-4 rounded-md px-2 py-1 text-[10px] font-bold tracking-wider"
            :class="
              index % 2 === 0
                ? 'bg-primary text-primary-container-foreground'
                : 'border border-white/20 bg-background/60 text-foreground backdrop-blur-sm'
            "
            >{{ index % 2 === 0 ? "4K" : "HD" }}</span
          >
          <div class="absolute inset-x-0 bottom-0 p-5">
            <h2 class="font-display truncate text-xl font-medium">
              {{ title.title }}
            </h2>
            <p class="mt-1 text-xs text-tertiary">
              {{ title.year }} · {{ title.genre }}
            </p>
          </div>
        </NuxtLink>
      </div>
      <p
        v-else-if="!isLoading && !loadError"
        class="mt-10 rounded-3xl border border-white/10 bg-surface-container p-10 text-center text-muted-foreground"
      >
        {{ copy.empty }}
      </p>
      <button
        v-if="visibleTitles.length"
        class="mx-auto mt-12 block rounded-full border border-white/10 bg-surface-container px-6 py-3 text-xs font-medium text-foreground transition hover:border-primary/60 hover:text-primary"
      >
        {{ copy.showMore }}
      </button>
    </section>

    <footer
      class="border-t border-white/5 bg-surface-container-lowest px-5 py-10 text-center text-xs text-tertiary"
    >
      <NuxtLink to="/" class="font-display text-xl font-semibold text-primary"
        >ZMovie</NuxtLink
      >
      <p class="mt-5">© 2026 ZMovie Premium. All rights reserved.</p>
    </footer>
  </main>
</template>
