<script setup lang="ts">
import { MessageSquare, Send, Smile } from "@lucide/vue";

const props = defineProps<{
  reviews?: {
    items?: Array<{
      id: string;
      authorName: string;
      rating: number;
      comment?: string;
      updatedAt: string;
    }>;
  };
  actionNotice?: string | null;
  isSubmittingReview: boolean;
  formattedDate: (date: string) => string;
}>();

const rating = defineModel<number>("rating", { default: 10 });
const comment = defineModel<string>("comment", { default: "" });

const emit = defineEmits<{
  (e: "submit"): void;
}>();

const commentTab = ref<"comment" | "rating">("comment");
const isSpoiler = ref(false);
</script>

<template>
  <section class="mt-8 pt-8 border-t border-white/10">
    <!-- Comment Header with Tabs -->
    <div class="flex items-center justify-between mb-5">
      <h3
        class="text-base sm:text-lg font-bold text-white flex items-center gap-2 font-display"
      >
        <MessageSquare class="size-5 text-primary" />
        <span>Bình luận & Đánh giá ({{ reviews?.items?.length || 0 }})</span>
      </h3>

      <div
        class="flex items-center rounded-xl border border-white/10 bg-[#191b24] p-1 text-xs font-bold"
      >
        <button
          type="button"
          class="rounded-lg px-3 py-1.5 transition"
          :class="
            commentTab === 'comment'
              ? 'bg-white text-black shadow-sm'
              : 'text-white/70 hover:text-white'
          "
          @click="commentTab = 'comment'"
        >
          Bình luận
        </button>
        <button
          type="button"
          class="rounded-lg px-3 py-1.5 transition"
          :class="
            commentTab === 'rating'
              ? 'bg-white text-black shadow-sm'
              : 'text-white/70 hover:text-white'
          "
          @click="commentTab = 'rating'"
        >
          Đánh giá
        </button>
      </div>
    </div>

    <!-- Comment Form Box -->
    <div
      class="rounded-3xl border border-white/10 bg-[#141622] p-4 sm:p-5 shadow-lg"
    >
      <!-- Star Rating selector when in Rating tab -->
      <div
        v-if="commentTab === 'rating'"
        class="mb-4 flex flex-wrap items-center gap-2 border-b border-white/8 pb-3 text-xs"
      >
        <span class="font-semibold text-white/80">Điểm đánh giá:</span>
        <div class="flex items-center gap-1">
          <button
            v-for="star in 10"
            :key="star"
            type="button"
            class="text-sm transition hover:scale-125"
            :class="star <= (rating || 10) ? 'text-amber-400' : 'text-white/20'"
            @click="rating = star"
          >
            ★
          </button>
        </div>
        <span class="font-bold text-amber-400">{{ rating || 10 }}/10</span>
      </div>

      <p
        v-if="actionNotice"
        class="mb-3 rounded-xl bg-primary/10 px-3 py-2 text-xs font-semibold text-primary"
      >
        {{ actionNotice }}
      </p>
      <textarea
        v-model="comment"
        rows="3"
        placeholder="Viết bình luận..."
        maxlength="1000"
        class="w-full resize-none bg-transparent text-sm text-white placeholder-white/40 outline-none"
      />

      <div
        class="mt-3 flex flex-wrap items-center justify-between gap-3 border-t border-white/5 pt-3 text-xs text-white/60"
      >
        <!-- Left options -->
        <div class="flex items-center gap-4">
          <label class="flex items-center gap-2 cursor-pointer select-none">
            <input
              v-model="isSpoiler"
              type="checkbox"
              class="size-4 rounded border-white/20 bg-white/10 text-primary focus:ring-0"
            />
            <span>Tiết lộ?</span>
          </label>
          <button
            type="button"
            class="flex items-center gap-1.5 hover:text-white transition"
          >
            <Smile class="size-4 text-amber-400" />
            <span>Popo</span>
          </button>
        </div>

        <!-- Right actions -->
        <div class="flex items-center gap-3">
          <span class="text-[11px] text-muted-foreground">
            {{ (comment || "").length }} / 1000
          </span>
          <button
            type="button"
            :disabled="isSubmittingReview || !comment?.trim()"
            class="inline-flex items-center gap-1.5 rounded-xl bg-primary px-4 py-2 font-bold text-black shadow transition hover:bg-[#ffde8a] active:scale-95 disabled:opacity-40"
            @click="emit('submit')"
          >
            <span>Gửi</span>
            <Send class="size-3.5" />
          </button>
        </div>
      </div>
    </div>

    <!-- Comments List -->
    <div v-if="reviews?.items?.length" class="mt-6 flex flex-col gap-4">
      <article
        v-for="review in reviews.items"
        :key="review.id"
        class="flex items-start gap-3.5 rounded-2xl bg-[#191b24]/60 p-4 border border-white/5"
      >
        <span
          class="grid size-10 shrink-0 place-items-center rounded-full bg-primary/20 text-xs font-bold text-primary"
        >
          {{ (review.authorName || "U").slice(0, 2).toUpperCase() }}
        </span>
        <div class="flex-1 min-w-0">
          <div class="flex flex-wrap items-center gap-2 text-xs">
            <span class="font-bold text-white">{{ review.authorName }}</span>
            <span
              class="inline-flex items-center gap-1 rounded bg-amber-500/15 px-1.5 py-0.5 text-[10px] font-bold text-amber-400"
            >
              ★ {{ review.rating }}/10
            </span>
            <span class="text-white/40">{{
              formattedDate(review.updatedAt)
            }}</span>
          </div>
          <p class="mt-1.5 text-xs sm:text-sm text-gray-200 leading-relaxed">
            {{
              review.comment || `Đã chấm điểm ${review.rating}/10 cho bộ phim.`
            }}
          </p>
        </div>
      </article>
    </div>
    <div
      v-else
      class="mt-6 rounded-2xl border border-dashed border-white/10 p-8 text-center text-xs text-muted-foreground"
    >
      Chưa có bình luận nào. Hãy là người đầu tiên chia sẻ cảm nghĩ về bộ phim!
    </div>
  </section>
</template>
