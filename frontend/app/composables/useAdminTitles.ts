import { onBeforeUnmount, onMounted, ref } from "vue";
import {
  deleteAdminTitle,
  fetchAdminGenres,
  fetchAdminTitle,
  fetchAdminTitles,
  toggleAdminTitleFeatured,
  updateAdminTitle,
} from "~/services/admin.service";
import type {
  AdminGenreSummary,
  AdminTitleDetail,
  AdminTitleEdit,
  AdminTitleSummary,
  Paged,
} from "~/types/admin";

function readApiMessage(error: unknown, fallback: string): string {
  const problem = (
    error as { data?: { title?: string; errors?: { description?: string }[] } }
  )?.data;
  return problem?.errors?.[0]?.description ?? problem?.title ?? fallback;
}

export function useAdminTitles() {
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
      const response = await fetchAdminTitles({
        q: search.value.trim() || undefined,
        genre: genreFilter.value || undefined,
        type: typeFilter.value || undefined,
        featured: featuredFilter.value || undefined,
        page: page.value,
        pageSize: 20,
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
    try {
      genres.value = await fetchAdminGenres();
    } catch {
      genres.value = [];
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

  function changePage(next: number) {
    page.value = next;
    void load();
  }

  async function openEditor(item: AdminTitleSummary) {
    formError.value = "";
    try {
      const detail = await fetchAdminTitle(item.slug);
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
      await updateAdminTitle(editing.value.slug, form.value);
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
      const updated = await toggleAdminTitleFeatured(item.slug, !item.featured);
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
      await deleteAdminTitle(target.slug);
      errorMessage.value = "";
      notice.value = `Đã xoá "${target.vietnameseTitle}".`;
      deleteTarget.value = null;
      if (result.value && result.value.items.length === 1 && page.value > 1) {
        page.value -= 1;
      }
      await load();
    } catch {
      notice.value = "";
      errorMessage.value = "Không xoá được phim.";
    } finally {
      isDeleting.value = false;
    }
  }

  onMounted(() => {
    void load();
    void loadGenres();
  });

  onBeforeUnmount(() => {
    clearTimeout(searchTimer);
  });

  return {
    search,
    genreFilter,
    typeFilter,
    featuredFilter,
    page,
    result,
    genres,
    pending,
    errorMessage,
    notice,
    editing,
    form,
    isSaving,
    formError,
    deleteTarget,
    isDeleting,
    featuredPending,
    scheduleSearch,
    applyFilters,
    changePage,
    openEditor,
    closeEditor,
    saveTitle,
    toggleFeatured,
    confirmDelete,
  };
}
