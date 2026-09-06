<script setup lang="ts">
import { AlertTriangle, CheckCircle2, Clock3, Send, X } from "@lucide/vue";
import { reportTitleIssue } from "~/services/catalog.service";

const props = withDefaults(
  defineProps<{
    isOpen: boolean;
    movieSlug: string;
    movieTitle?: string;
    episodeNumber?: number | null;
    currentTime?: number;
  }>(),
  {
    movieTitle: "",
    episodeNumber: null,
    currentTime: 0,
  },
);

const emit = defineEmits<{
  close: [];
}>();

const categories = [
  { value: "broken_video", label: "Video bị lỗi / Không phát được" },
  { value: "broken_audio", label: "Mất tiếng / Âm thanh bị rè" },
  { value: "wrong_subtitles", label: "Lỗi phụ đề / Sai thuyết minh" },
  { value: "wrong_episode", label: "Sai tập / Trùng lặp tập phim" },
  { value: "other", label: "Vấn đề khác" },
];

const selectedCategory = ref("broken_video");
const description = ref("");
const includeTimestamp = ref(true);
const sending = ref(false);
const submitted = ref(false);
const error = ref("");

function formatTime(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = Math.floor(seconds % 60);
  return `${m.toString().padStart(2, "0")}:${s.toString().padStart(2, "0")}`;
}

async function submitReport() {
  if (sending.value || !props.movieSlug) return;
  sending.value = true;
  error.value = "";
  try {
    await reportTitleIssue(props.movieSlug, {
      category: selectedCategory.value,
      description:
        description.value.trim() ||
        categories.find((c) => c.value === selectedCategory.value)?.label ||
        "Báo lỗi phim",
      timestampSeconds: includeTimestamp.value
        ? Math.floor(props.currentTime || 0)
        : undefined,
    });
    submitted.value = true;
    setTimeout(() => {
      submitted.value = false;
      description.value = "";
      emit("close");
    }, 1800);
  } catch {
    error.value = "Không thể gửi báo lỗi. Vui lòng thử lại sau.";
  } finally {
    sending.value = false;
  }
}
</script>

<template>
  <div
    v-if="isOpen"
    class="fixed inset-0 z-50 grid place-items-center bg-black/80 p-4 backdrop-blur-sm"
    @click.self="emit('close')"
  >
    <div
      class="w-full max-w-lg rounded-3xl border border-white/10 bg-[#191b24] p-6 shadow-2xl sm:p-8"
    >
      <div class="flex items-start justify-between gap-4">
        <div class="flex items-center gap-3">
          <div
            class="grid size-10 place-items-center rounded-2xl bg-amber-500/15 text-amber-400"
          >
            <AlertTriangle class="size-5" />
          </div>
          <div>
            <h3 class="font-display text-lg font-bold text-white">
              Báo lỗi phim
            </h3>
            <p class="text-xs text-muted-foreground">
              {{ movieTitle || movieSlug }}
              <span v-if="episodeNumber"> · Tập {{ episodeNumber }}</span>
            </p>
          </div>
        </div>
        <button
          type="button"
          class="grid size-8 place-items-center rounded-full text-white/50 transition hover:bg-white/10 hover:text-white"
          aria-label="Đóng"
          @click="emit('close')"
        >
          <X class="size-4" />
        </button>
      </div>

      <div
        v-if="submitted"
        class="mt-6 rounded-2xl border border-emerald-500/30 bg-emerald-500/10 p-6 text-center text-emerald-200"
      >
        <CheckCircle2 class="mx-auto size-10 text-emerald-400" />
        <h4 class="mt-3 font-bold">Cảm ơn bạn đã phản hồi!</h4>
        <p class="mt-1 text-xs text-emerald-300/80">
          Đội ngũ kỹ thuật sẽ kiểm tra và khắc phục sự cố này sớm nhất.
        </p>
      </div>

      <form v-else class="mt-6 space-y-4" @submit.prevent="submitReport">
        <div>
          <label class="mb-2 block text-xs font-semibold text-white/70">
            Loại sự cố gặp phải
          </label>
          <div class="space-y-2">
            <label
              v-for="cat in categories"
              :key="cat.value"
              class="flex cursor-pointer items-center gap-3 rounded-xl border border-white/8 bg-black/20 p-3 transition hover:border-primary/40"
              :class="
                selectedCategory === cat.value
                  ? 'border-primary/60 bg-primary/10'
                  : ''
              "
            >
              <input
                v-model="selectedCategory"
                type="radio"
                name="issue_category"
                :value="cat.value"
                class="accent-primary"
              />
              <span class="text-xs font-medium text-white">{{
                cat.label
              }}</span>
            </label>
          </div>
        </div>

        <label
          v-if="currentTime > 0"
          class="flex cursor-pointer items-center gap-2 rounded-xl bg-white/5 p-3 text-xs text-muted-foreground"
        >
          <input
            v-model="includeTimestamp"
            type="checkbox"
            class="accent-primary"
          />
          <Clock3 class="size-3.5 text-primary" />
          <span>Đính kèm mốc thời gian hiện tại:</span>
          <strong class="font-mono text-primary">{{
            formatTime(currentTime)
          }}</strong>
        </label>

        <div>
          <label class="mb-2 block text-xs font-semibold text-white/70">
            Chi tiết thêm (tùy chọn)
          </label>
          <textarea
            v-model="description"
            rows="3"
            placeholder="Mô tả cụ thể triệu chứng, server đang xem hoặc thiết bị đang dùng..."
            class="w-full rounded-xl border border-white/10 bg-black/25 p-3 text-xs text-white outline-none transition focus:border-primary/60"
          />
        </div>

        <p
          v-if="error"
          class="rounded-xl bg-destructive/15 px-3 py-2 text-xs font-semibold text-destructive"
        >
          {{ error }}
        </p>

        <div class="flex items-center justify-end gap-3 pt-2">
          <button
            type="button"
            class="rounded-xl px-4 py-2.5 text-xs font-semibold text-white/70 transition hover:bg-white/10 hover:text-white"
            @click="emit('close')"
          >
            Hủy bỏ
          </button>
          <button
            type="submit"
            class="inline-flex items-center gap-2 rounded-xl bg-primary px-5 py-2.5 text-xs font-bold text-primary-foreground transition hover:brightness-110 disabled:opacity-50"
            :disabled="sending"
          >
            <Send class="size-3.5" />
            <span>{{ sending ? "Đang gửi..." : "Gửi báo lỗi" }}</span>
          </button>
        </div>
      </form>
    </div>
  </div>
</template>
