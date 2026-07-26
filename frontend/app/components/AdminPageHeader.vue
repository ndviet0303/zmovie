<script setup lang="ts">
defineProps<{ title: string; description?: string }>();
</script>

<template>
  <div class="flex flex-wrap items-end justify-between gap-4">
    <div>
      <h1 class="font-display text-3xl font-extrabold tracking-tight">
        {{ title }}
      </h1>
      <p
        v-if="description"
        class="mt-2 max-w-2xl text-sm text-muted-foreground"
      >
        {{ description }}
      </p>
    </div>
    <div class="flex flex-wrap items-center gap-3"><slot name="actions" /></div>
  </div>
</template>
