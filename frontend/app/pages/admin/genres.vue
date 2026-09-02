<script setup lang="ts">
import { Check, Pencil, Trash2, X } from "@lucide/vue";

definePageMeta({ layout: "admin", middleware: "admin" });
useHead({ title: "Thể loại — ZMovie admin" });

const {
  genres,
  pending,
  errorMessage,
  notice,
  newSlug,
  newName,
  isCreating,
  isSlugManual,
  editingId,
  editingName,
  isSaving,
  deleteTarget,
  isDeleting,
  createGenre,
  startEdit,
  cancelEdit,
  saveEdit,
  confirmDelete,
} = useAdminGenres();
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
