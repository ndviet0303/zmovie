import { onMounted, ref, watch } from "vue";
import {
  createAdminGenre,
  deleteAdminGenre,
  fetchAdminGenres,
  updateAdminGenre,
} from "~/services/admin.service";
import type { AdminGenreSummary } from "~/types/admin";

function slugify(value: string): string {
  return value
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/[đĐ]/g, "d")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

function readApiMessage(error: unknown, fallback: string): string {
  const problem = (
    error as { data?: { title?: string; errors?: { description?: string }[] } }
  )?.data;
  return problem?.errors?.[0]?.description ?? problem?.title ?? fallback;
}

export function useAdminGenres() {
  const genres = ref<AdminGenreSummary[]>([]);
  const pending = ref(true);
  const errorMessage = ref("");
  const notice = ref("");

  const newSlug = ref("");
  const newName = ref("");
  const isCreating = ref(false);
  const isSlugManual = ref(false);

  const editingId = ref<string | null>(null);
  const editingName = ref("");
  const isSaving = ref(false);

  const deleteTarget = ref<AdminGenreSummary | null>(null);
  const isDeleting = ref(false);

  watch(newName, (value) => {
    if (!isSlugManual.value) newSlug.value = slugify(value);
  });

  async function load() {
    pending.value = true;
    errorMessage.value = "";
    try {
      genres.value = await fetchAdminGenres();
    } catch {
      errorMessage.value = "Không tải được danh sách thể loại.";
    } finally {
      pending.value = false;
    }
  }

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
      await createAdminGenre({ slug, name });
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
      const updated = await updateAdminGenre(genre.id, { name });
      Object.assign(genre, updated);
      notice.value = `Đã đổi tên thành "${updated.name}".`;
      cancelEdit();
    } catch (error: unknown) {
      notice.value = "";
      errorMessage.value = readApiMessage(
        error,
        "Không đổi được tên thể loại.",
      );
    } finally {
      isSaving.value = false;
    }
  }

  async function confirmDelete() {
    if (!deleteTarget.value || isDeleting.value) return;
    isDeleting.value = true;
    try {
      await deleteAdminGenre(deleteTarget.value.id);
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

  onMounted(() => {
    void load();
  });

  return {
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
  };
}
