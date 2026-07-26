<script setup lang="ts">
import { Trash2 } from "@lucide/vue";
import type { AdminReviewSummary, Paged } from "~/types/admin";

definePageMeta({ layout: "admin", middleware: "admin" });
useHead({ title: "Đánh giá — ZMovie admin" });

const { $api } = useNuxtApp();

const search = ref("");
const maxRating = ref("");
const page = ref(1);
const result = ref<Paged<AdminReviewSummary> | null>(null);
const pending = ref(false);
const errorMessage = ref("");
const notice = ref("");
const deleteTarget = ref<AdminReviewSummary | null>(null);
const isDeleting = ref(false);

let searchTimer: ReturnType<typeof setTimeout> | undefined;
let requestSeq = 0;

async function load() {
  const token = ++requestSeq;
  pending.value = true;
  errorMessage.value = "";
  try {
    const response = await $api<Paged<AdminReviewSummary>>(
      "/v1/admin/reviews",
      {
        credentials: "include",
        query: {
          q: search.value.trim() || undefined,
          maxRating: maxRating.value || undefined,
          page: page.value,
          pageSize: 20,
        },
      },
    );
    if (token !== requestSeq) return;
    result.value = response;
  } catch {
    if (token !== requestSeq) return;
    errorMessage.value = "Không tải được danh sách đánh giá.";
  } finally {
    if (token === requestSeq) pending.value = false;
  }
}

function scheduleSearch() {
  clearTimeout(searchTimer);
  searchTimer = setTimeout(() => {
    page.value = 1;
    void load();
  }, 300);
}

function applyFilters() {
  page.value = 1;
  void load();
}

async function confirmDelete() {
  if (!deleteTarget.value || isDeleting.value) return;
  isDeleting.value = true;
  try {
    await $api(`/v1/admin/reviews/${deleteTarget.value.id}`, {
      method: "DELETE",
      credentials: "include",
    });
    notice.value = `Đã gỡ đánh giá của ${deleteTarget.value.authorName}.`;
    deleteTarget.value = null;
    await load();
  } catch {
    notice.value = "";
    errorMessage.value = "Không gỡ được đánh giá.";
  } finally {
    isDeleting.value = false;
  }
}

function formatDate(value: string) {
  return new Date(value).toLocaleString("vi-VN");
}

onMounted(() => void load());
onBeforeUnmount(() => clearTimeout(searchTimer));
</script>

<template>
  <div class="space-y-6">
    <AdminPageHeader
      title="Kiểm duyệt đánh giá"
      description="Lọc theo điểm thấp để tìm nhanh nội dung cần xem xét, và gỡ đánh giá vi phạm."
    />

    <div class="grid gap-3 sm:grid-cols-[1fr_auto]">
      <input
        v-model="search"
        type="search"
        placeholder="Tìm theo người viết hoặc nội dung…"
        class="h-11 rounded-xl border border-border bg-input px-4 text-sm outline-none transition focus:border-primary"
        @input="scheduleSearch"
      />
      <select
        v-model="maxRating"
        class="h-11 rounded-xl border border-border bg-input px-4 text-sm outline-none transition focus:border-primary"
        @change="applyFilters"
      >
        <option value="">Mọi mức điểm</option>
        <option value="3">Từ 3 điểm trở xuống</option>
        <option value="5">Từ 5 điểm trở xuống</option>
        <option value="7">Từ 7 điểm trở xuống</option>
      </select>
    </div>

    <p
      v-if="notice"
      class="rounded-2xl bg-primary/10 px-4 py-3 text-sm font-semibold text-primary"
    >
      {{ notice }}
    </p>
    <p
      v-if="errorMessage"
      class="rounded-2xl bg-destructive/10 px-4 py-3 text-sm text-destructive"
    >
      {{ errorMessage }}
    </p>

    <div
      class="overflow-hidden rounded-2xl border border-white/10 bg-surface-container"
    >
      <p
        v-if="pending && !result"
        class="px-4 py-8 text-center text-sm text-muted-foreground"
      >
        Đang tải…
      </p>
      <p
        v-else-if="!result?.items.length"
        class="px-4 py-8 text-center text-sm text-muted-foreground"
      >
        Không có đánh giá nào khớp bộ lọc.
      </p>
      <ul v-else class="divide-y divide-white/5">
        <li
          v-for="item in result.items"
          :key="item.id"
          class="flex flex-wrap items-start gap-4 px-4 py-4"
        >
          <div class="min-w-0 flex-1">
            <div class="flex flex-wrap items-center gap-2">
              <Badge :variant="item.rating <= 4 ? 'destructive' : 'outline'">
                {{ item.rating }}/10
              </Badge>
              <span class="text-sm font-semibold">{{ item.authorName }}</span>
              <span class="text-xs text-muted-foreground">·</span>
              <NuxtLink
                v-if="item.titleSlug"
                :to="`/movies/${item.titleSlug}`"
                class="text-xs font-semibold text-primary transition hover:underline"
              >
                {{ item.titleName }}
              </NuxtLink>
              <span v-else class="text-xs text-muted-foreground">
                {{ item.titleName }}
              </span>
            </div>
            <p
              v-if="item.comment"
              class="mt-2 whitespace-pre-line text-sm text-muted-foreground"
            >
              {{ item.comment }}
            </p>
            <p v-else class="mt-2 text-sm italic text-muted-foreground">
              (chỉ chấm điểm, không có nhận xét)
            </p>
            <p class="mt-2 text-xs text-muted-foreground">
              {{ formatDate(item.updatedAt) }}
            </p>
          </div>
          <Button
            size="icon-xs"
            variant="destructive"
            title="Gỡ đánh giá"
            @click="deleteTarget = item"
          >
            <Trash2 />
          </Button>
        </li>
      </ul>

      <AdminPagination
        v-if="result"
        :page="result.page"
        :page-count="result.pageCount"
        :total="result.total"
        :pending="pending"
        @change="
          (next) => {
            page = next;
            load();
          }
        "
      />
    </div>

    <AdminModal
      v-if="deleteTarget"
      alert
      label="Xác nhận gỡ đánh giá"
      @close="deleteTarget = null"
    >
      <div>
        <h2 class="font-display text-lg font-bold">Gỡ đánh giá này?</h2>
        <p class="mt-3 text-sm text-muted-foreground">
          Đánh giá {{ deleteTarget.rating }}/10 của
          <strong class="text-foreground">{{ deleteTarget.authorName }}</strong>
          cho "{{ deleteTarget.titleName }}" sẽ bị xoá vĩnh viễn.
        </p>
        <div class="mt-6 flex justify-end gap-3">
          <Button variant="outline" size="sm" @click="deleteTarget = null">
            Huỷ
          </Button>
          <Button
            variant="destructive"
            size="sm"
            :disabled="isDeleting"
            @click="confirmDelete"
          >
            {{ isDeleting ? "Đang gỡ…" : "Gỡ đánh giá" }}
          </Button>
        </div>
      </div>
    </AdminModal>
  </div>
</template>
