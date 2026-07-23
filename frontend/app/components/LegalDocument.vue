<script setup lang="ts">
defineProps<{
  eyebrow: string
  title: string
  description: string
  sections: { heading: string, paragraphs: string[] }[]
}>()
</script>

<template>
  <main class="min-h-screen bg-background text-foreground">
    <AppNavbar :locale="'vi'" />
    <section class="border-b border-white/7 bg-surface-container-low">
      <div class="mx-auto max-w-240 px-5 py-16 lg:px-12 lg:py-22">
        <p class="text-xs font-semibold uppercase tracking-[.18em] text-primary">{{ eyebrow }}</p>
        <h1 class="font-display mt-4 max-w-3xl text-5xl font-semibold leading-[1.03] tracking-[-.04em] lg:text-6xl">{{ title }}</h1>
        <p class="mt-6 max-w-2xl text-base leading-7 text-muted-foreground lg:text-lg">{{ description }}</p>
        <p class="mt-7 text-xs text-tertiary">Cập nhật lần cuối: 23 tháng 7, 2026</p>
      </div>
    </section>
    <section class="mx-auto grid max-w-240 gap-12 px-5 py-14 lg:grid-cols-[12rem_minmax(0,1fr)] lg:px-12 lg:py-20">
      <aside class="hidden lg:block"><p class="text-xs font-semibold uppercase tracking-[.14em] text-tertiary">Nội dung</p><nav class="mt-5 space-y-3"><a v-for="(section, index) in sections" :key="section.heading" :href="`#section-${index + 1}`" class="block text-sm text-muted-foreground transition hover:text-primary">{{ index + 1 }}. {{ section.heading }}</a></nav></aside>
      <article class="max-w-3xl">
        <section v-for="(section, index) in sections" :id="`section-${index + 1}`" :key="section.heading" class="scroll-mt-28 border-b border-white/7 py-9 first:pt-0 last:border-0 last:pb-0">
          <p class="text-xs font-semibold text-primary">{{ String(index + 1).padStart(2, '0') }}</p>
          <h2 class="font-display mt-3 text-3xl font-semibold tracking-[-.02em]">{{ section.heading }}</h2>
          <p v-for="paragraph in section.paragraphs" :key="paragraph" class="mt-5 text-[15px] leading-7 text-muted-foreground">{{ paragraph }}</p>
        </section>
      </article>
    </section>
    <footer class="border-t border-white/5 bg-surface-container-lowest px-5 py-10 text-center lg:px-12"><NuxtLink to="/" class="font-display text-sm font-semibold text-primary">ZMovie</NuxtLink><p class="mt-4 text-xs text-tertiary">© 2026 ZMovie Premium. All rights reserved.</p></footer>
  </main>
</template>
