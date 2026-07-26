<script setup lang="ts">
import { ChevronLeft, ChevronRight } from "@lucide/vue";

const props = defineProps<{
  page: number;
  pageCount: number;
  total: number;
  pending?: boolean;
}>();
const emit = defineEmits<{ change: [page: number] }>();

const canGoBack = computed(() => props.page > 1 && !props.pending);
const canGoForward = computed(
  () => props.page < props.pageCount && !props.pending,
);
</script>

<template>
  <div
    v-if="total > 0"
    class="flex flex-wrap items-center justify-between gap-3 border-t border-white/10 px-4 py-3"
  >
    <p class="text-xs text-muted-foreground">
      Trang {{ page }}/{{ Math.max(pageCount, 1) }} ·
      {{ total.toLocaleString("vi-VN") }} mục
    </p>
    <div class="flex items-center gap-2">
      <Button
        size="icon-xs"
        variant="outline"
        :disabled="!canGoBack"
        aria-label="Trang trước"
        @click="emit('change', page - 1)"
      >
        <ChevronLeft />
      </Button>
      <Button
        size="icon-xs"
        variant="outline"
        :disabled="!canGoForward"
        aria-label="Trang sau"
        @click="emit('change', page + 1)"
      >
        <ChevronRight />
      </Button>
    </div>
  </div>
</template>
