import { onBeforeUnmount, onMounted, ref } from "vue";
import { deleteAdminReview, fetchAdminReviews } from "~/services/admin.service";
import type { AdminReviewSummary, Paged } from "~/types/admin";

export function useAdminReviews() {
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
      const response = await fetchAdminReviews({
        q: search.value.trim() || undefined,
        maxRating: maxRating.value || undefined,
        page: page.value,
        pageSize: 20,
      });
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

  function changePage(next: number) {
    page.value = next;
    void load();
  }

  async function confirmDelete() {
    if (!deleteTarget.value || isDeleting.value) return;
    isDeleting.value = true;
    try {
      await deleteAdminReview(deleteTarget.value.id);
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

  onMounted(() => {
    void load();
  });

  onBeforeUnmount(() => {
    clearTimeout(searchTimer);
  });

  return {
    search,
    maxRating,
    page,
    result,
    pending,
    errorMessage,
    notice,
    deleteTarget,
    isDeleting,
    load,
    scheduleSearch,
    applyFilters,
    changePage,
    confirmDelete,
    formatDate,
  };
}
