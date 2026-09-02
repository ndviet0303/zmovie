<script setup lang="ts">
import { ChevronRight } from "@lucide/vue";

const topics = [
  {
    title: "Top IMDb",
    query: { sort: "rating" },
    gradient: "from-amber-500 via-amber-600 to-amber-700",
  },
  {
    title: "Thuyết Minh",
    query: { genre: "Thuyết Minh" },
    gradient: "from-teal-500 via-emerald-600 to-teal-700",
  },
  {
    title: "Phim 4K",
    query: { collection: "recommended" },
    gradient: "from-indigo-500 via-purple-600 to-violet-700",
  },
  {
    title: "Lồng Tiếng Cực Mạnh",
    query: { genre: "Lồng Tiếng" },
    gradient: "from-purple-500 via-fuchsia-600 to-purple-800",
  },
  {
    title: "Netflix",
    query: { country: "Âu Mỹ" },
    gradient: "from-rose-500 via-red-600 to-rose-800",
  },
  {
    title: "TVB",
    query: { country: "Hồng Kông" },
    gradient: "from-emerald-500 via-teal-600 to-emerald-700",
  },
];
</script>

<template>
  <section class="mx-auto max-w-360 px-4 sm:px-6 lg:px-10 pt-2 pb-8">
    <h2
      class="text-lg sm:text-xl font-bold tracking-tight text-white mb-3 font-display"
    >
      Bạn đang quan tâm gì?
    </h2>

    <div
      class="flex items-center gap-3.5 overflow-x-auto pb-3 pt-1 no-scrollbar scroll-smooth"
    >
      <NuxtLink
        v-for="item in topics"
        :key="item.title"
        :to="{ path: '/browse', query: item.query }"
        class="group relative flex h-28 min-w-[160px] sm:min-w-[185px] flex-col justify-between rounded-2xl p-4 shadow-lg transition-transform duration-300 hover:-translate-y-1 active:scale-[0.98] bg-gradient-to-br"
        :class="item.gradient"
      >
        <h3
          class="text-base sm:text-lg font-extrabold text-white leading-snug drop-shadow-sm font-display"
        >
          {{ item.title }}
        </h3>
        <span
          class="inline-flex items-center gap-1 text-xs font-semibold text-white/90 group-hover:text-white transition"
        >
          Xem chủ đề
          <ChevronRight
            class="size-3.5 stroke-[2.5] transition-transform group-hover:translate-x-0.5"
          />
        </span>
      </NuxtLink>

      <!-- +4 chủ đề card -->
      <NuxtLink
        to="/genres"
        class="flex h-28 min-w-[140px] items-center justify-center rounded-2xl border border-white/10 bg-[#191b24] p-4 text-sm font-bold text-white/90 shadow-md transition hover:border-primary hover:text-primary active:scale-[0.98]"
      >
        +4 chủ đề
      </NuxtLink>
    </div>
  </section>
</template>
