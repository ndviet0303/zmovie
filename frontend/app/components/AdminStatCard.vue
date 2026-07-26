<script setup lang="ts">
const props = defineProps<{
  label: string;
  value: number | string;
  hint?: string;
}>();

const display = computed(() =>
  typeof props.value === "number"
    ? props.value.toLocaleString("vi-VN")
    : props.value,
);
</script>

<template>
  <div
    class="rounded-2xl border border-white/10 bg-surface-container p-5 transition hover:border-primary/30"
  >
    <p
      class="text-xs font-semibold uppercase tracking-wide text-muted-foreground"
    >
      {{ label }}
    </p>
    <p class="mt-2 font-display text-3xl font-extrabold">{{ display }}</p>
    <p v-if="hint" class="mt-1 text-xs text-muted-foreground">{{ hint }}</p>
  </div>
</template>
