<script setup lang="ts">
import { Pencil, Star, Trash2 } from "@lucide/vue";
import type {
  AdminGenreSummary,
  AdminTitleDetail,
  AdminTitleEdit,
  AdminTitleSummary,
  Paged,
} from "~/types/admin";

definePageMeta({ layout: "admin", middleware: "admin" });
useHead({ title: "Quản lý phim — ZMovie admin" });

const { $api } = useNuxtApp();

const search = ref("");
const genreFilter = ref("");
const typeFilter = ref("");
const featuredFilter = ref("");
const page = ref(1);
const result = ref<Paged<AdminTitleSummary> | null>(null);
const genres = ref<AdminGenreSummary[]>([]);
const pending = ref(false);
const errorMessage = ref("");
const notice = ref("");

const editing = ref<AdminTitleDetail | null>(null);
const form = ref<AdminTitleEdit | null>(null);
const isSaving = ref(false);
const formError = ref("");
const deleteTarget = ref<AdminTitleSummary | null>(null);
const isDeleting = ref(false);
const featuredPending = ref(new Set<string>());

let searchTimer: ReturnType<typeof setTimeout> | undefined;
// Monotonic token: a slow response for an old query must never overwrite a newer one.
let requestSeq = 0;

async function load() {
  const token = ++requestSeq;
  pending.value = true;
  errorMessage.value = "";
  try {
    const response = await $api<Paged<AdminTitleSummary>>("/v1/admin/titles", {
      credentials: "include",
      query: {
        q: search.value.trim() || undefined,
        genre: genreFilter.value || undefined,
        type: typeFilter.value || undefined,
        featured: featuredFilter.value || undefined,
        page: page.value,
        pageSize: 20,
      },
    });
    if (token !== requestSeq) return;
    result.value = response;
  } catch {
    if (token !== requestSeq) return;
    errorMessage.value = "Không tải được danh sách phim.";
  } finally {
    if (token === requestSeq) pending.value = false;
  }
}

async function loadGenres() {
  genres.value = await $api<AdminGenreSummary[]>("/v1/admin/genres", {
    credentials: "include",
  }).catch(() => []);
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

function changePage(next: number) {
  page.value = next;
  void load();
}

async function openEditor(item: AdminTitleSummary) {
  formError.value = "";
  try {
    const detail = await $api<AdminTitleDetail>(
      `/v1/admin/titles/${encodeURIComponent(item.slug)}`,
      { credentials: "include" },
    );
    editing.value = detail;
    form.value = {
      vietnameseTitle: detail.vietnameseTitle,
      englishTitle: detail.englishTitle,
      vietnameseSynopsis: detail.vietnameseSynopsis,
      englishSynopsis: detail.englishSynopsis,
      genre: detail.genre,
      year: detail.year,
      type: detail.type,
      posterUrl: detail.posterUrl,
      runtimeMinutes: detail.runtimeMinutes,
      featured: detail.featured,
    };
  } catch {
    errorMessage.value = "Không mở được phim này.";
  }
}

function closeEditor() {
  editing.value = null;
  form.value = null;
  formError.value = "";
}

async function saveTitle() {
  if (!editing.value || !form.value || isSaving.value) return;
  isSaving.value = true;
  formError.value = "";
  try {
    await $api<AdminTitleDetail>(
      `/v1/admin/titles/${encodeURIComponent(editing.value.slug)}`,
      { method: "PUT", credentials: "include", body: form.value },
    );
    errorMessage.value = "";
    notice.value = `Đã lưu "${form.value.vietnameseTitle}".`;
    closeEditor();
    await load();
  } catch (error: unknown) {
    formError.value = readApiMessage(error, "Không lưu được thay đổi.");
  } finally {
    isSaving.value = false;
  }
}

async function toggleFeatured(item: AdminTitleSummary) {
  if (featuredPending.value.has(item.slug)) return;
  featuredPending.value.add(item.slug);
  notice.value = "";
  errorMessage.value = "";
  try {
    // Trust the server's value rather than a locally negated one, so a double
    // click or a concurrent edit cannot leave the row out of sync.
    const updated = await $api<AdminTitleDetail>(
      `/v1/admin/titles/${encodeURIComponent(item.slug)}/featured`,
      {
        method: "PATCH",
        credentials: "include",
        body: { featured: !item.featured },
      },
    );
    item.featured = updated.featured;
  } catch {
    errorMessage.value = "Không đổi được trạng thái nổi bật.";
  } finally {
    featuredPending.value.delete(item.slug);
  }
}

async function confirmDelete() {
  if (!deleteTarget.value || isDeleting.value) return;
  isDeleting.value = true;
  const target = deleteTarget.value;
  try {
    await $api(`/v1/admin/titles/${encodeURIComponent(target.slug)}`, {
      method: "DELETE",
      credentials: "include",
    });
    errorMessage.value = "";
    notice.value = `Đã xoá "${target.vietnameseTitle}".`;
    deleteTarget.value = null;
    // Removing the last row of the last page would otherwise strand the admin on an
    // out-of-range page that renders the "no results" empty state.
    if (result.value && result.value.items.length === 1 && page.value > 1)
      page.value -= 1;
    await load();
  } catch {
    notice.value = "";
    errorMessage.value = "Không xoá được phim.";
  } finally {
    isDeleting.value = false;
  }
}

function readApiMessage(error: unknown, fallback: string) {
  const problem = (
    error as { data?: { title?: string; errors?: { description?: string }[] } }
  )?.data;
  return problem?.errors?.[0]?.description ?? problem?.title ?? fallback;
}

onMounted(() => {
  void load();
  void loadGenres();
});

onBeforeUnmount(() => clearTimeout(searchTimer));
</script>

<template>
  <div class="space-y-6">
    <AdminPageHeader
      title="Quản lý phim"
      description="Sửa thông tin, bật/tắt nổi bật và xoá phim khỏi catalog."
    />

    <div class="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
      <input
        v-model="search"
        type="search"
        placeholder="Tìm theo tên hoặc slug…"
        class="h-11 rounded-xl border border-border bg-input px-4 text-sm outline-none transition focus:border-primary"
        @input="scheduleSearch"
      />
      <select
        v-model="genreFilter"
        class="h-11 rounded-xl border border-border bg-input px-4 text-sm outline-none transition focus:border-primary"
        @change="applyFilters"
      >
        <option value="">Tất cả thể loại</option>
        <option v-for="genre in genres" :key="genre.id" :value="genre.name">
          {{ genre.name }}
        </option>
      </select>
      <select
        v-model="typeFilter"
        class="h-11 rounded-xl border border-border bg-input px-4 text-sm outline-none transition focus:border-primary"
        @change="applyFilters"
      >
        <option value="">Tất cả loại</option>
        <option value="movie">Phim lẻ</option>
        <option value="series">Phim bộ</option>
      </select>
      <select
        v-model="featuredFilter"
        class="h-11 rounded-xl border border-border bg-input px-4 text-sm outline-none transition focus:border-primary"
        @change="applyFilters"
      >
        <option value="">Nổi bật: tất cả</option>
        <option value="true">Chỉ nổi bật</option>
        <option value="false">Không nổi bật</option>
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
      <div class="overflow-x-auto">
        <table class="w-full min-w-200 text-left text-sm">
          <thead
            class="border-b border-white/10 text-xs uppercase text-muted-foreground"
          >
            <tr>
              <th class="px-4 py-3 font-semibold">Phim</th>
              <th class="px-4 py-3 font-semibold">Thể loại</th>
              <th class="px-4 py-3 font-semibold">Năm</th>
              <th class="px-4 py-3 font-semibold">Loại</th>
              <th class="px-4 py-3 font-semibold">Tập</th>
              <th class="px-4 py-3 text-right font-semibold">Thao tác</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-white/5">
            <tr v-if="pending && !result">
              <td
                colspan="6"
                class="px-4 py-8 text-center text-muted-foreground"
              >
                Đang tải…
              </td>
            </tr>
            <tr v-else-if="!result?.items.length">
              <td
                colspan="6"
                class="px-4 py-8 text-center text-muted-foreground"
              >
                Không có phim nào khớp bộ lọc.
              </td>
            </tr>
            <tr
              v-for="item in result?.items ?? []"
              :key="item.id"
              class="transition hover:bg-white/5"
            >
              <td class="px-4 py-3">
                <div class="flex items-center gap-3">
                  <img
                    :src="item.posterUrl"
                    :alt="item.vietnameseTitle"
                    loading="lazy"
                    class="h-14 w-10 shrink-0 rounded-lg object-cover"
                  />
                  <div class="min-w-0">
                    <p class="truncate font-semibold">
                      {{ item.vietnameseTitle }}
                    </p>
                    <p class="truncate text-xs text-muted-foreground">
                      {{ item.slug }}
                    </p>
                  </div>
                </div>
              </td>
              <td class="px-4 py-3 text-muted-foreground">{{ item.genre }}</td>
              <td class="px-4 py-3 text-muted-foreground">{{ item.year }}</td>
              <td class="px-4 py-3">
                <Badge variant="outline">
                  {{ item.type === "series" ? "Phim bộ" : "Phim lẻ" }}
                </Badge>
              </td>
              <td class="px-4 py-3 text-muted-foreground">
                {{ item.episodeCount }}
              </td>
              <td class="px-4 py-3">
                <div class="flex items-center justify-end gap-2">
                  <Button
                    size="icon-xs"
                    :variant="item.featured ? 'default' : 'outline'"
                    :aria-pressed="item.featured"
                    :disabled="featuredPending.has(item.slug)"
                    :title="item.featured ? 'Bỏ nổi bật' : 'Đánh dấu nổi bật'"
                    @click="toggleFeatured(item)"
                  >
                    <Star />
                  </Button>
                  <Button
                    size="icon-xs"
                    variant="outline"
                    title="Sửa"
                    @click="openEditor(item)"
                  >
                    <Pencil />
                  </Button>
                  <Button
                    size="icon-xs"
                    variant="destructive"
                    title="Xoá"
                    @click="deleteTarget = item"
                  >
                    <Trash2 />
                  </Button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <AdminPagination
        v-if="result"
        :page="result.page"
        :page-count="result.pageCount"
        :total="result.total"
        :pending="pending"
        @change="changePage"
      />
    </div>

    <!-- Editor -->
    <AdminModal
      v-if="editing && form"
      label="Sửa thông tin phim"
      panel-class="max-h-[90vh] max-w-3xl"
      @close="closeEditor"
    >
      <div>
        <div class="flex items-start justify-between gap-4">
          <div>
            <h2 class="font-display text-xl font-bold">
              {{ editing.vietnameseTitle }}
            </h2>
            <p class="mt-1 text-xs text-muted-foreground">
              {{ editing.slug }} ·
              {{ editing.viewCount.toLocaleString("vi-VN") }} lượt xem ·
              {{ editing.reviewCount }} đánh giá
            </p>
          </div>
          <Button size="sm" variant="ghost" @click="closeEditor">Đóng</Button>
        </div>

        <div class="mt-6 grid gap-4 sm:grid-cols-2">
          <label class="grid gap-2 text-sm font-semibold">
            Tên tiếng Việt
            <input
              v-model="form.vietnameseTitle"
              class="h-11 rounded-xl border border-border bg-input px-4 font-normal outline-none focus:border-primary"
            />
          </label>
          <label class="grid gap-2 text-sm font-semibold">
            Tên tiếng Anh
            <input
              v-model="form.englishTitle"
              class="h-11 rounded-xl border border-border bg-input px-4 font-normal outline-none focus:border-primary"
            />
          </label>
          <label class="grid gap-2 text-sm font-semibold">
            Thể loại
            <input
              v-model="form.genre"
              list="admin-genre-options"
              class="h-11 rounded-xl border border-border bg-input px-4 font-normal outline-none focus:border-primary"
            />
          </label>
          <label class="grid gap-2 text-sm font-semibold">
            Loại
            <select
              v-model="form.type"
              class="h-11 rounded-xl border border-border bg-input px-4 font-normal outline-none focus:border-primary"
            >
              <option value="movie">Phim lẻ</option>
              <option value="series">Phim bộ</option>
            </select>
          </label>
          <label class="grid gap-2 text-sm font-semibold">
            Năm
            <input
              v-model.number="form.year"
              type="number"
              min="1888"
              max="2100"
              class="h-11 rounded-xl border border-border bg-input px-4 font-normal outline-none focus:border-primary"
            />
          </label>
          <label class="grid gap-2 text-sm font-semibold">
            Thời lượng (phút)
            <input
              v-model.number="form.runtimeMinutes"
              type="number"
              min="0"
              class="h-11 rounded-xl border border-border bg-input px-4 font-normal outline-none focus:border-primary"
            />
          </label>
          <label class="grid gap-2 text-sm font-semibold sm:col-span-2">
            Poster URL
            <input
              v-model="form.posterUrl"
              class="h-11 rounded-xl border border-border bg-input px-4 font-normal outline-none focus:border-primary"
            />
          </label>
          <label class="grid gap-2 text-sm font-semibold sm:col-span-2">
            Mô tả tiếng Việt
            <textarea
              v-model="form.vietnameseSynopsis"
              rows="4"
              class="rounded-xl border border-border bg-input px-4 py-3 font-normal outline-none focus:border-primary"
            />
          </label>
          <label class="grid gap-2 text-sm font-semibold sm:col-span-2">
            Mô tả tiếng Anh
            <textarea
              v-model="form.englishSynopsis"
              rows="4"
              class="rounded-xl border border-border bg-input px-4 py-3 font-normal outline-none focus:border-primary"
            />
          </label>
          <label
            class="flex cursor-pointer items-center gap-3 rounded-xl border border-border bg-input px-4 py-3 text-sm font-semibold sm:col-span-2"
          >
            <input
              v-model="form.featured"
              type="checkbox"
              class="size-4 accent-primary"
            />
            Hiển thị ở khu vực nổi bật
          </label>
        </div>

        <datalist id="admin-genre-options">
          <option v-for="genre in genres" :key="genre.id" :value="genre.name" />
        </datalist>

        <p
          v-if="formError"
          class="mt-4 rounded-xl bg-destructive/10 px-4 py-3 text-sm text-destructive"
        >
          {{ formError }}
        </p>

        <div class="mt-6 flex justify-end gap-3">
          <Button variant="outline" size="sm" @click="closeEditor">Huỷ</Button>
          <Button size="sm" :disabled="isSaving" @click="saveTitle">
            {{ isSaving ? "Đang lưu…" : "Lưu thay đổi" }}
          </Button>
        </div>
      </div>
    </AdminModal>

    <!-- Delete confirmation -->
    <AdminModal
      v-if="deleteTarget"
      alert
      label="Xác nhận xoá phim"
      @close="deleteTarget = null"
    >
      <div>
        <h2 class="font-display text-lg font-bold">Xoá phim này?</h2>
        <p class="mt-3 text-sm text-muted-foreground">
          <strong class="text-foreground">{{
            deleteTarget.vietnameseTitle
          }}</strong>
          cùng toàn bộ tập phim, lượt xem, đánh giá và lịch sử xem liên quan sẽ
          bị xoá vĩnh viễn.
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
            {{ isDeleting ? "Đang xoá…" : "Xoá vĩnh viễn" }}
          </Button>
        </div>
      </div>
    </AdminModal>
  </div>
</template>
