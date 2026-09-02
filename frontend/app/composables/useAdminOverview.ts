import { onMounted, ref } from "vue";
import { fetchAdminOverview } from "~/services/admin.service";
import type { AdminOverview } from "~/types/admin";

export function useAdminOverview() {
  const overview = ref<AdminOverview | null>(null);
  const pending = ref(true);
  const errorMessage = ref("");

  async function load() {
    pending.value = true;
    errorMessage.value = "";
    try {
      overview.value = await fetchAdminOverview();
    } catch {
      errorMessage.value = "Không tải được số liệu tổng quan.";
    } finally {
      pending.value = false;
    }
  }

  function formatDate(value: string) {
    return new Date(value).toLocaleDateString("vi-VN");
  }

  onMounted(() => {
    void load();
  });

  return {
    overview,
    pending,
    errorMessage,
    load,
    formatDate,
  };
}
