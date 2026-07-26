<script setup lang="ts">
import type { AdminUserSummary, Paged, UserRole } from "~/types/admin";

definePageMeta({ layout: "admin", middleware: "admin" });
useHead({ title: "Người dùng — ZMovie admin" });

const { $api } = useNuxtApp();
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
    const response = await $api<Paged<AdminUserSummary>>("/v1/admin/users", {
      credentials: "include",
      query: {
        q: search.value.trim() || undefined,
        role: roleFilter.value || undefined,
        page: page.value,
        pageSize: 20,
      },
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

async function setRole(item: AdminUserSummary, role: UserRole) {
  if (savingId.value) return;
  savingId.value = item.id;
  errorMessage.value = "";
  notice.value = "";
  try {
    const updated = await $api<AdminUserSummary>(
      `/v1/admin/users/${item.id}/role`,
      { method: "PATCH", credentials: "include", body: { role } },
    );
    Object.assign(item, updated);
    notice.value = `Đã đổi quyền của ${updated.displayName} thành ${role === "admin" ? "quản trị viên" : "thành viên"}.`;
  } catch (error: unknown) {
    const problem = (
      error as {
        data?: { title?: string; errors?: { description?: string }[] };
      }
    )?.data;
    notice.value = "";
    errorMessage.value =
      problem?.errors?.[0]?.description ??
      problem?.title ??
      "Không đổi được quyền.";
  } finally {
    savingId.value = null;
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
      title="Người dùng"
      description="Cấp hoặc thu hồi quyền quản trị. Không thể tự thu quyền của chính mình, và luôn phải còn ít nhất một quản trị viên."
    />

    <div class="grid gap-3 sm:grid-cols-[1fr_auto]">
      <input
        v-model="search"
        type="search"
        placeholder="Tìm theo email hoặc tên…"
        class="h-11 rounded-xl border border-border bg-input px-4 text-sm outline-none transition focus:border-primary"
        @input="scheduleSearch"
      />
      <select
        v-model="roleFilter"
        class="h-11 rounded-xl border border-border bg-input px-4 text-sm outline-none transition focus:border-primary"
        @change="applyFilters"
      >
        <option value="">Tất cả quyền</option>
        <option value="admin">Quản trị viên</option>
        <option value="member">Thành viên</option>
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
              <th class="px-4 py-3 font-semibold">Người dùng</th>
              <th class="px-4 py-3 font-semibold">Quyền</th>
              <th class="px-4 py-3 font-semibold">Tham gia</th>
              <th class="px-4 py-3 font-semibold">Đăng nhập gần nhất</th>
              <th class="px-4 py-3 text-right font-semibold">Thao tác</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-white/5">
            <tr v-if="pending && !result">
              <td
                colspan="5"
                class="px-4 py-8 text-center text-muted-foreground"
              >
                Đang tải…
              </td>
            </tr>
            <tr v-else-if="!result?.items.length">
              <td
                colspan="5"
                class="px-4 py-8 text-center text-muted-foreground"
              >
                Không có người dùng nào khớp bộ lọc.
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
                    v-if="item.avatarUrl"
                    :src="item.avatarUrl"
                    :alt="item.displayName"
                    loading="lazy"
                    referrerpolicy="no-referrer"
                    class="size-9 shrink-0 rounded-full object-cover"
                  />
                  <div class="min-w-0">
                    <p class="truncate font-semibold">
                      {{ item.displayName }}
                      <span
                        v-if="item.id === currentUser?.id"
                        class="ml-1 text-xs font-normal text-muted-foreground"
                        >(bạn)</span
                      >
                    </p>
                    <p class="truncate text-xs text-muted-foreground">
                      {{ item.email }}
                    </p>
                  </div>
                </div>
              </td>
              <td class="px-4 py-3">
                <Badge :variant="item.role === 'admin' ? 'default' : 'outline'">
                  {{ item.role === "admin" ? "Quản trị viên" : "Thành viên" }}
                </Badge>
              </td>
              <td class="px-4 py-3 text-xs text-muted-foreground">
                {{ formatDate(item.createdAt) }}
              </td>
              <td class="px-4 py-3 text-xs text-muted-foreground">
                {{ formatDate(item.lastSignedInAt) }}
              </td>
              <td class="px-4 py-3 text-right">
                <Button
                  v-if="item.role === 'member'"
                  size="xs"
                  variant="outline"
                  :disabled="savingId === item.id"
                  @click="setRole(item, 'admin')"
                >
                  Cấp quyền admin
                </Button>
                <Button
                  v-else
                  size="xs"
                  variant="destructive"
                  :disabled="
                    savingId === item.id || item.id === currentUser?.id
                  "
                  :title="
                    item.id === currentUser?.id
                      ? 'Không thể tự thu quyền của chính mình'
                      : undefined
                  "
                  @click="setRole(item, 'member')"
                >
                  Thu quyền admin
                </Button>
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
        @change="
          (next) => {
            page = next;
            load();
          }
        "
      />
    </div>
  </div>
</template>
