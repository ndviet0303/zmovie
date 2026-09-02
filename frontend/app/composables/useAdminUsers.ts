import { onBeforeUnmount, onMounted, ref } from "vue";
import { fetchAdminUsers, updateAdminUserRole } from "~/services/admin.service";
import type { AdminUserSummary, Paged, UserRole } from "~/types/admin";

function readApiError(error: unknown, fallback: string): string {
  const problem = (
    error as {
      data?: { title?: string; errors?: { description?: string }[] };
    }
  )?.data;
  return problem?.errors?.[0]?.description ?? problem?.title ?? fallback;
}

export function useAdminUsers() {
  const { user: currentUser } = useAuthSession();

  const search = ref("");
  const roleFilter = ref<"" | UserRole>("");
  const page = ref(1);
  const result = ref<Paged<AdminUserSummary> | null>(null);
  const pending = ref(false);
  const errorMessage = ref("");
  const notice = ref("");
  const savingId = ref<string | null>(null);

  let searchTimer: ReturnType<typeof setTimeout> | undefined;
  let requestSeq = 0;

  async function load() {
    const token = ++requestSeq;
    pending.value = true;
    errorMessage.value = "";
    try {
      const response = await fetchAdminUsers({
        q: search.value.trim() || undefined,
        role: roleFilter.value || undefined,
        page: page.value,
        pageSize: 20,
      });
      if (token !== requestSeq) return;
      result.value = response;
    } catch {
      if (token !== requestSeq) return;
      errorMessage.value = "Không tải được danh sách người dùng.";
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

  async function setRole(item: AdminUserSummary, role: UserRole) {
    if (savingId.value) return;
    savingId.value = item.id;
    errorMessage.value = "";
    notice.value = "";
    try {
      const updated = await updateAdminUserRole(item.id, role);
      Object.assign(item, updated);
      notice.value = `Đã đổi quyền của ${updated.displayName} thành ${role === "admin" ? "quản trị viên" : "thành viên"}.`;
    } catch (error: unknown) {
      notice.value = "";
      errorMessage.value = readApiError(error, "Không đổi được quyền.");
    } finally {
      savingId.value = null;
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
    currentUser,
    search,
    roleFilter,
    page,
    result,
    pending,
    errorMessage,
    notice,
    savingId,
    load,
    scheduleSearch,
    applyFilters,
    changePage,
    setRole,
    formatDate,
  };
}
