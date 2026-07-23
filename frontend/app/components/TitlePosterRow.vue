<script setup lang="ts">
type Title = {
  slug: string
  title: string
  genre: string
  year: number
  type: string
  posterUrl: string
}

defineProps<{
  titles: Title[]
}>()
</script>

<template>
  <div class="grid grid-cols-2 gap-5 sm:grid-cols-3 lg:grid-cols-5 lg:gap-6">
    <NuxtLink v-for="(title, index) in titles" :key="title.slug" :to="`/movies/${title.slug}`" class="group relative aspect-[2/3] overflow-hidden rounded-3xl bg-surface-container shadow-[inset_0_1px_0_rgba(235,225,214,.1)] transition duration-300 hover:scale-[1.02] hover:shadow-[0_16px_40px_rgba(217,131,103,.16)]">
      <img :src="title.posterUrl" :alt="title.title" class="absolute inset-0 size-full object-cover opacity-80 transition duration-500 group-hover:opacity-100" loading="lazy" />
      <div class="absolute inset-0 bg-gradient-to-t from-black/95 via-black/20 to-transparent" />
      <span class="absolute left-4 top-4 rounded-md px-2 py-1 text-[10px] font-bold tracking-wider" :class="index % 2 === 0 ? 'bg-primary text-primary-container-foreground' : 'border border-white/20 bg-background/60 text-foreground backdrop-blur-sm'">{{ title.type === 'series' ? 'SERIES' : 'HD' }}</span>
      <div class="absolute inset-x-0 bottom-0 p-5">
        <h3 class="font-display truncate text-xl font-medium text-foreground">{{ title.title }}</h3>
        <p class="mt-1 text-xs text-tertiary">{{ title.year }} · {{ title.genre }}</p>
      </div>
    </NuxtLink>
  </div>
</template>
