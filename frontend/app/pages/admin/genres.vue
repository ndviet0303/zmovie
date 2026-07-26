<script setup lang="ts">
import { Check, Pencil, Trash2, X } from "@lucide/vue";
import type { AdminGenreSummary } from "~/types/admin";

definePageMeta({ layout: "admin", middleware: "admin" });
useHead({ title: "Thể loại — ZMovie admin" });

const { $api } = useNuxtApp();

const genres = ref<AdminGenreSummary[]>([]);
const pending = ref(true);
const errorMessage = ref("");
const notice = ref("");

const newSlug = ref("");
const newName = ref("");
const isCreating = ref(false);

const editingId = ref<string | null>(null);
const editingName = ref("");
const isSaving = ref(false);

const deleteTarget = ref<AdminGenreSummary | null>(null);
const isDeleting = ref(false);

async function load() {
  pending.value = true;
  errorMessage.value = "";
  try {
    genres.value = await $api<AdminGenreSummary[]>("/v1/admin/genres", {
      credentials: "include",
    });
  } catch {
    errorMessage.value = "Không tải được danh sách thể loại.";
  } finally {
    pending.value = false;
  }
}

function slugify(value: string) {
  return value
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/[đĐ]/g, "d")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

// Mirror the name into the slug until the operator edits the slug themselves.
const isSlugManual = ref(false);
watch(newName, (value) => {
  if (!isSlugManual.value) newSlug.value = slugify(value);
});

async function createGenre() {
  if (isCreating.value) return;
  const slug = newSlug.value.trim();
  const name = newName.value.trim();
  if (!slug || !name) {
    errorMessage.value = "Cần nhập cả tên và slug.";
    return;
  }
  isCreating.value = true;
  errorMessage.value = "";
  notice.value = "";
  try {
    await $api<AdminGenreSummary>("/v1/admin/genres", {
      method: "POST",
      credentials: "include",
      body: { slug, name },
    });
    notice.value = `Đã thêm thể loại "${name}".`;
    newName.value = "";
    newSlug.value = "";
    isSlugManual.value = false;
    await load();
  } catch (error: unknown) {
    notice.value = "";
    errorMessage.value = readApiMessage(error, "Không thêm được thể loại.");
  } finally {
    isCreating.value = false;
  }
}

function startEdit(genre: AdminGenreSummary) {
  editingId.value = genre.id;
  editingName.value = genre.name;
}

function cancelEdit() {
  editingId.value = null;
  editingName.value = "";
}

async function saveEdit(genre: AdminGenreSummary) {
  if (isSaving.value) return;
  const name = editingName.value.trim();
  if (!name) return;
  isSaving.value = true;
  errorMessage.value = "";
  try {
    const updated = await $api<AdminGenreSummary>(
      `/v1/admin/genres/${genre.id}`,
      {
        method: "PUT",
        credentials: "include",
        body: { name },
      },
    );
    Object.assign(genre, updated);
    notice.value = `Đã đổi tên thành "${updated.name}".`;
    cancelEdit();
  } catch (error: unknown) {
    notice.value = "";
    errorMessage.value = readApiMessage(error, "Không đổi được tên thể loại.");
  } finally {
    isSaving.value = false;
  }
}

async function confirmDelete() {
  if (!deleteTarget.value || isDeleting.value) return;
  isDeleting.value = true;
  try {
    await $api(`/v1/admin/genres/${deleteTarget.value.id}`, {
      method: "DELETE",
      credentials: "include",
    });
    notice.value = `Đã xoá thể loại "${deleteTarget.value.name}".`;
    deleteTarget.value = null;
    await load();
  } catch {
    notice.value = "";
    errorMessage.value = "Không xoá được thể loại.";
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

onMounted(() => void load());
</script>

<template>
  <div class="space-y-6">
    <AdminPageHeader
      title="Thể loại"
      description="Danh mục thể loại dùng cho bộ lọc. Số phim được đếm theo tên thể loại lưu trên từng phim."
    />

    <form
      class="grid gap-3 rounded-2xl border border-white/10 bg-surface-container p-5 sm:grid-cols-[1fr_1fr_auto]"
      @submit.prevent="createGenre"
    >
      <label class="grid gap-2 text-sm font-semibold">
        Tên hiển thị
        <input
          v-model="newName"
          placeholder="Ví dụ: Kinh dị"
          class="h-11 rounded-xl border border-border bg-input px-4 font-normal outline-none focus:border-primary"
        />
      </label>
      <label class="grid gap-2 text-sm font-semibold">
        Slug
        <input
          v-model="newSlug"
          placeholder="kinh-di"
          class="h-11 rounded-xl border border-border bg-input px-4 font-normal outline-none focus:border-primary"
          @input="isSlugManual = true"
        />
      </label>
      <div class="flex items-end">
        <Button type="submit" size="sm" :disabled="isCreating">
          {{ isCreating ? "Đang thêm…" : "Thêm thể loại" }}
        </Button>
      </div>
    </form>

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
        v-if="pending"
        class="px-4 py-8 text-center text-sm text-muted-foreground"
      >
        Đang tải…
      </p>
      <p
        v-else-if="!genres.length"
        class="px-4 py-8 text-center text-sm text-muted-foreground"
      >
        Chưa có thể loại nào.
      </p>
      <ul v-else class="divide-y divide-white/5">
        <li
          v-for="genre in genres"
          :key="genre.id"
          class="flex flex-wrap items-center gap-4 px-4 py-3"
        >
          <div class="min-w-0 flex-1">
            <template v-if="editingId === genre.id">
              <input
                v-model="editingName"
                class="h-10 w-full max-w-sm rounded-xl border border-border bg-input px-3 text-sm outline-none focus:border-primary"
                @keydown.enter.prevent="saveEdit(genre)"
                @keydown.esc="cancelEdit"
              />
            </template>
            <template v-else>
              <p class="truncate text-sm font-semibold">{{ genre.name }}</p>
              <p class="truncate text-xs text-muted-foreground">
                {{ genre.slug }}
              </p>
            </template>
          </div>
          <Badge variant="outline">{{ genre.titleCount }} phim</Badge>
          <div class="flex items-center gap-2">
            <template v-if="editingId === genre.id">
              <Button
                size="icon-xs"
                :disabled="isSaving"
                title="Lưu"
                @click="saveEdit(genre)"
              >
                <Check />
              </Button>
              <Button
                size="icon-xs"
                variant="outline"
                title="Huỷ"
                @click="cancelEdit"
              >
                <X />
              </Button>
            </template>
            <template v-else>
              <Button
                size="icon-xs"
                variant="outline"
                title="Đổi tên"
                @click="startEdit(genre)"
              >
                <Pencil />
              </Button>
              <Button
                size="icon-xs"
                variant="destructive"
                title="Xoá"
                @click="deleteTarget = genre"
              >
                <Trash2 />
              </Button>
            </template>
          </div>
        </li>
      </ul>
    </div>

    <AdminModal
      v-if="deleteTarget"
      alert
      label="Xác nhận xoá thể loại"
      @close="deleteTarget = null"
    >
      <div>
        <h2 class="font-display text-lg font-bold">Xoá thể loại này?</h2>
        <p class="mt-3 text-sm text-muted-foreground">
          Xoá
          <strong class="text-foreground">{{ deleteTarget.name }}</strong> chỉ
          gỡ mục khỏi danh mục lọc. {{ deleteTarget.titleCount }} phim đang mang
          tên thể loại này vẫn giữ nguyên giá trị đó.
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
            {{ isDeleting ? "Đang xoá…" : "Xoá thể loại" }}
          </Button>
        </div>
      </div>
    </AdminModal>
  </div>
</template>
