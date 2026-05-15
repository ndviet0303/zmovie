<script setup>
import {
  ArrowLeft,
  CheckCircle2,
  Film,
  LayoutDashboard,
  LogOut,
  Pencil,
  Plus,
  RefreshCw,
  Save,
  Search,
  Trash2,
  X,
} from 'lucide-vue-next'
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import logoUrl from './assets/zmovie-logo.png'
import { absoluteAssetUrl, adminApi, fetchLookups } from './services/api'

const SESSION_KEY = 'zmovie_admin_session'

const route = useRoute()
const router = useRouter()

const session = ref(readSession())
const loginForm = reactive({
  email: 'admin@zmovie.local',
  password: 'password',
})
const movieForm = reactive(blankMovie())
const movies = ref([])
const lookups = ref(null)
const providers = ref([])
const demoAccounts = ref([])
const selectedDemoEmail = ref('admin@zmovie.local')
const editingMovie = ref(null)
const searchQuery = ref('')
const statusFilter = ref('')
const isLoading = ref(false)
const isSaving = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const isLoginView = computed(() => route.name === 'admin-login' || !session.value)
const canManageMovies = computed(() => session.value?.permissions?.includes('movies.manage'))
const publishedCount = computed(() => movies.value.filter((movie) => movie.status === 'published').length)
const draftCount = computed(() => movies.value.filter((movie) => movie.status === 'draft').length)
const clearedCount = computed(() => movies.value.filter((movie) => movie.rights_status === 'cleared').length)
const filteredMovies = computed(() => {
  const q = searchQuery.value.trim().toLowerCase()

  return movies.value.filter((movie) => {
    const matchesStatus = !statusFilter.value || movie.status === statusFilter.value
    const matchesQuery =
      !q ||
      [movie.title, movie.original_title, movie.slug]
        .filter(Boolean)
        .some((value) => String(value).toLowerCase().includes(q))

    return matchesStatus && matchesQuery
  })
})

function readSession() {
  try {
    return JSON.parse(window.localStorage.getItem(SESSION_KEY) ?? 'null')
  } catch {
    return null
  }
}

function saveSession(payload) {
  const nextSession = {
    accessToken: payload.access_token,
    tokenType: payload.token_type ?? 'Bearer',
    user: payload.user,
    permissions: payload.permissions ?? [],
  }
  window.localStorage.setItem(SESSION_KEY, JSON.stringify(nextSession))
  session.value = nextSession
}

function blankMovie() {
  return {
    title: '',
    original_title: '',
    slug: '',
    type: 'movie',
    status: 'draft',
    rights_status: 'cleared',
    release_year: new Date().getFullYear(),
    runtime_minutes: 90,
    overview: '',
    poster_path: '',
    backdrop_path: '',
    trailer_url: '',
    content_provider_id: '',
    genre_ids: [],
    country_ids: [],
    language_ids: [],
    is_featured: false,
  }
}

function resetForm() {
  Object.assign(movieForm, blankMovie())
  editingMovie.value = null
  errorMessage.value = ''
  successMessage.value = ''
}

function slugify(value) {
  return value
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/đ/g, 'd')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
}

function ensureSlug() {
  if (!movieForm.slug && movieForm.title) {
    movieForm.slug = slugify(movieForm.title)
  }
}

function normalizeAdminMovie(movie) {
  return {
    ...movie,
    genre_ids: movie.genres?.map((genre) => genre.id) ?? [],
    country_ids: movie.countries?.map((country) => country.id) ?? [],
    language_ids: movie.languages?.map((language) => language.id) ?? [],
  }
}

function editMovie(movie) {
  const normalized = normalizeAdminMovie(movie)
  Object.assign(movieForm, blankMovie(), {
    title: normalized.title ?? '',
    original_title: normalized.original_title ?? '',
    slug: normalized.slug ?? '',
    type: normalized.type ?? 'movie',
    status: normalized.status ?? 'draft',
    rights_status: normalized.rights_status ?? 'cleared',
    release_year: normalized.release_year ?? '',
    runtime_minutes: normalized.runtime_minutes ?? '',
    overview: normalized.overview ?? '',
    poster_path: normalized.poster_path ?? '',
    backdrop_path: normalized.backdrop_path ?? '',
    trailer_url: normalized.trailer_url ?? '',
    content_provider_id: normalized.content_provider_id ?? '',
    genre_ids: normalized.genre_ids,
    country_ids: normalized.country_ids,
    language_ids: normalized.language_ids,
    is_featured: Boolean(normalized.is_featured),
  })
  editingMovie.value = movie
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

function formPayload() {
  ensureSlug()

  return {
    title: movieForm.title,
    original_title: movieForm.original_title || null,
    slug: movieForm.slug,
    type: movieForm.type,
    status: movieForm.status,
    rights_status: movieForm.rights_status,
    release_year: movieForm.release_year ? Number(movieForm.release_year) : null,
    runtime_minutes: movieForm.runtime_minutes ? Number(movieForm.runtime_minutes) : null,
    overview: movieForm.overview || null,
    poster_path: movieForm.poster_path || null,
    backdrop_path: movieForm.backdrop_path || null,
    trailer_url: movieForm.trailer_url || null,
    content_provider_id: movieForm.content_provider_id ? Number(movieForm.content_provider_id) : null,
    genre_ids: movieForm.genre_ids.map(Number),
    country_ids: movieForm.country_ids.map(Number),
    language_ids: movieForm.language_ids.map(Number),
    is_featured: movieForm.is_featured,
  }
}

async function login() {
  isLoading.value = true
  errorMessage.value = ''

  try {
    const payload = await adminApi.login(loginForm)
    saveSession(payload)
    await router.push({ name: 'admin' })
    await loadAdminData()
  } catch (error) {
    errorMessage.value = error.message
  } finally {
    isLoading.value = false
  }
}

function selectDemoAccount(email) {
  const account = demoAccounts.value.find((item) => item.email === email)
  if (!account) return

  loginForm.email = account.email
  loginForm.password = account.password
  selectedDemoEmail.value = account.email
}

async function loadDemoAccounts() {
  try {
    demoAccounts.value = await adminApi.demoAccounts()
    selectDemoAccount(selectedDemoEmail.value)
  } catch {
    demoAccounts.value = [
      {
        label: 'Super Admin',
        email: 'admin@zmovie.local',
        password: 'password',
        role: 'super-admin',
        description: 'Toàn quyền hệ thống.',
      },
    ]
  }
}

async function logout() {
  try {
    await adminApi.logout()
  } catch {
    // Local logout should still work if the token is already expired or revoked.
  }

  window.localStorage.removeItem(SESSION_KEY)
  session.value = null
  router.push({ name: 'admin-login' })
}

async function loadAdminData() {
  if (!session.value) return

  isLoading.value = true
  errorMessage.value = ''

  try {
    const [moviePayload, lookupPayload, providerPayload] = await Promise.all([
      adminApi.listMovies({ status: statusFilter.value || undefined }),
      fetchLookups(),
      adminApi.listContentProviders({ per_page: 100 }).catch(() => ({ data: [] })),
    ])
    movies.value = moviePayload.data ?? moviePayload
    lookups.value = lookupPayload
    providers.value = providerPayload.data ?? providerPayload
  } catch (error) {
    errorMessage.value = error.message
  } finally {
    isLoading.value = false
  }
}

async function saveMovie() {
  isSaving.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    if (editingMovie.value) {
      await adminApi.updateMovie(editingMovie.value.id, formPayload())
      successMessage.value = 'Đã cập nhật phim.'
    } else {
      await adminApi.createMovie(formPayload())
      successMessage.value = 'Đã tạo phim mới.'
    }
    resetForm()
    await loadAdminData()
  } catch (error) {
    errorMessage.value = error.message
  } finally {
    isSaving.value = false
  }
}

async function publishMovie(movie) {
  if (movie.status === 'published') return

  try {
    await adminApi.publishMovie(movie.id)
    successMessage.value = `Đã publish ${movie.title}.`
    await loadAdminData()
  } catch (error) {
    errorMessage.value = error.message
  }
}

async function deleteMovie(movie) {
  if (!window.confirm(`Xóa phim "${movie.title}"?`)) return

  try {
    await adminApi.deleteMovie(movie.id)
    successMessage.value = 'Đã xóa phim.'
    await loadAdminData()
  } catch (error) {
    errorMessage.value = error.message
  }
}

onMounted(async () => {
  await loadDemoAccounts()

  if (session.value && route.name === 'admin-login') {
    await router.replace({ name: 'admin' })
  }
  await loadAdminData()
})
</script>

<template>
  <main class="min-h-screen bg-[#0d0f17] text-slate-100">
    <section v-if="isLoginView" class="grid min-h-screen place-items-center px-5">
      <form class="w-full max-w-md rounded-2xl border border-white/8 bg-[#171922] p-6 shadow-[0_24px_80px_rgba(0,0,0,0.35)]" @submit.prevent="login">
        <img class="h-16 w-48 object-contain object-left" :src="logoUrl" alt="ZMovie" />
        <h1 class="mt-8 text-2xl font-black text-white">Đăng nhập Admin</h1>
        <p class="mt-2 text-sm leading-6 text-slate-400">
          Chọn nhanh một tài khoản demo theo role để test phân quyền.
        </p>

        <label class="mt-6 block text-sm font-bold text-slate-300">
          Tài khoản demo
          <select
            v-model="selectedDemoEmail"
            class="admin-input"
            @change="selectDemoAccount(selectedDemoEmail)"
          >
            <option v-for="account in demoAccounts" :key="account.email" :value="account.email">
              {{ account.label }} - {{ account.email }}
            </option>
          </select>
        </label>
        <div
          v-if="demoAccounts.find((account) => account.email === selectedDemoEmail)"
          class="mt-3 rounded-xl border border-white/8 bg-white/6 p-3 text-sm leading-6 text-slate-300"
        >
          <p class="font-black text-white">
            {{ demoAccounts.find((account) => account.email === selectedDemoEmail).role }}
          </p>
          <p>{{ demoAccounts.find((account) => account.email === selectedDemoEmail).description }}</p>
          <p class="mt-1 text-xs font-bold text-[#ffe182]">Password: password</p>
        </div>

        <label class="mt-4 block text-sm font-bold text-slate-300">
          Email
          <input v-model="loginForm.email" class="mt-2 h-11 w-full rounded-lg border border-white/10 bg-white/6 px-3 text-white outline-none focus:border-[#ffe182]" type="email" />
        </label>
        <label class="mt-4 block text-sm font-bold text-slate-300">
          Mật khẩu
          <input v-model="loginForm.password" class="mt-2 h-11 w-full rounded-lg border border-white/10 bg-white/6 px-3 text-white outline-none focus:border-[#ffe182]" type="password" />
        </label>

        <p v-if="errorMessage" class="mt-4 rounded-lg bg-red-500/12 px-3 py-2 text-sm font-bold text-red-200">{{ errorMessage }}</p>

        <button class="mt-6 h-11 w-full rounded-lg bg-[#ffe182] font-black text-[#11131d] transition hover:bg-[#ffd058]" type="submit" :disabled="isLoading">
          {{ isLoading ? 'Đang đăng nhập...' : 'Đăng nhập' }}
        </button>
        <button class="mt-3 inline-flex items-center gap-2 text-sm font-bold text-slate-400 hover:text-white" type="button" @click="router.push({ name: 'home' })">
          <ArrowLeft :size="16" />
          Về trang xem phim
        </button>
      </form>
    </section>

    <section v-else class="mx-auto max-w-[1500px] px-4 py-5 md:px-8">
      <header class="flex flex-wrap items-center justify-between gap-4 border-b border-white/8 pb-5">
        <div class="flex items-center gap-4">
          <img class="h-12 w-40 object-contain object-left" :src="logoUrl" alt="ZMovie" />
          <div>
            <p class="text-xs font-black uppercase tracking-[0.18em] text-[#ffe182]">Admin Console</p>
            <h1 class="text-2xl font-black text-white">Quản lý phim</h1>
          </div>
        </div>
        <div class="flex flex-wrap items-center gap-2">
          <button class="inline-flex h-10 items-center gap-2 rounded-lg border border-white/10 bg-white/6 px-3 text-sm font-bold text-white hover:border-[#ffe182]" type="button" @click="router.push({ name: 'home' })">
            <ArrowLeft :size="16" />
            Trang phim
          </button>
          <button class="inline-flex h-10 items-center gap-2 rounded-lg bg-white/8 px-3 text-sm font-bold text-slate-200 hover:bg-white/14" type="button" @click="logout">
            <LogOut :size="16" />
            Đăng xuất
          </button>
        </div>
      </header>

      <div class="mt-6 grid gap-4 md:grid-cols-4">
        <div class="rounded-xl border border-white/8 bg-white/6 p-4">
          <LayoutDashboard class="text-[#ffe182]" :size="22" />
          <p class="mt-3 text-2xl font-black">{{ movies.length }}</p>
          <p class="text-sm text-slate-400">Tổng phim</p>
        </div>
        <div class="rounded-xl border border-white/8 bg-white/6 p-4">
          <CheckCircle2 class="text-emerald-300" :size="22" />
          <p class="mt-3 text-2xl font-black">{{ publishedCount }}</p>
          <p class="text-sm text-slate-400">Đã publish</p>
        </div>
        <div class="rounded-xl border border-white/8 bg-white/6 p-4">
          <Film class="text-sky-300" :size="22" />
          <p class="mt-3 text-2xl font-black">{{ draftCount }}</p>
          <p class="text-sm text-slate-400">Bản nháp</p>
        </div>
        <div class="rounded-xl border border-white/8 bg-white/6 p-4">
          <CheckCircle2 class="text-[#ffe182]" :size="22" />
          <p class="mt-3 text-2xl font-black">{{ clearedCount }}</p>
          <p class="text-sm text-slate-400">Đủ quyền</p>
        </div>
      </div>

      <p v-if="!canManageMovies" class="mt-5 rounded-xl bg-amber-400/12 p-4 text-sm font-bold text-amber-100">
        Tài khoản hiện tại thiếu quyền <code>movies.manage</code>.
      </p>
      <p v-if="errorMessage" class="mt-5 rounded-xl bg-red-500/12 p-4 text-sm font-bold text-red-100">{{ errorMessage }}</p>
      <p v-if="successMessage" class="mt-5 rounded-xl bg-emerald-500/12 p-4 text-sm font-bold text-emerald-100">{{ successMessage }}</p>

      <div class="mt-6 grid gap-6 xl:grid-cols-[420px_1fr]">
        <form class="rounded-2xl border border-white/8 bg-[#171922] p-5" @submit.prevent="saveMovie">
          <div class="flex items-center justify-between gap-3">
            <h2 class="text-lg font-black text-white">{{ editingMovie ? 'Sửa phim' : 'Thêm phim' }}</h2>
            <button v-if="editingMovie" class="rounded-lg bg-white/8 p-2 text-slate-300 hover:text-white" type="button" @click="resetForm">
              <X :size="18" />
            </button>
          </div>

          <div class="mt-5 grid gap-4">
            <label class="text-sm font-bold text-slate-300">
              Tên phim
              <input v-model="movieForm.title" class="admin-input" required @blur="ensureSlug" />
            </label>
            <label class="text-sm font-bold text-slate-300">
              Tên gốc
              <input v-model="movieForm.original_title" class="admin-input" />
            </label>
            <label class="text-sm font-bold text-slate-300">
              Slug URL
              <input v-model="movieForm.slug" class="admin-input" required />
            </label>
            <div class="grid grid-cols-2 gap-3">
              <label class="text-sm font-bold text-slate-300">
                Loại
                <select v-model="movieForm.type" class="admin-input">
                  <option value="movie">Phim lẻ</option>
                  <option value="series">Phim bộ</option>
                  <option value="short">TV Show</option>
                </select>
              </label>
              <label class="text-sm font-bold text-slate-300">
                Năm
                <input v-model="movieForm.release_year" class="admin-input" type="number" min="1888" max="2100" />
              </label>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <label class="text-sm font-bold text-slate-300">
                Trạng thái
                <select v-model="movieForm.status" class="admin-input">
                  <option value="draft">Draft</option>
                  <option value="published">Published</option>
                  <option value="archived">Archived</option>
                </select>
              </label>
              <label class="text-sm font-bold text-slate-300">
                Bản quyền
                <select v-model="movieForm.rights_status" class="admin-input">
                  <option value="cleared">Cleared</option>
                  <option value="pending">Pending</option>
                  <option value="unknown">Unknown</option>
                  <option value="expired">Expired</option>
                  <option value="disputed">Disputed</option>
                  <option value="blocked">Blocked</option>
                </select>
              </label>
            </div>
            <label class="text-sm font-bold text-slate-300">
              Provider
              <select v-model="movieForm.content_provider_id" class="admin-input">
                <option value="">Không chọn</option>
                <option v-for="provider in providers" :key="provider.id" :value="provider.id">{{ provider.name }}</option>
              </select>
            </label>
            <label class="text-sm font-bold text-slate-300">
              Poster path / URL
              <input v-model="movieForm.poster_path" class="admin-input" placeholder="ophim/.../poster.jpg" />
            </label>
            <label class="text-sm font-bold text-slate-300">
              Backdrop path / URL
              <input v-model="movieForm.backdrop_path" class="admin-input" placeholder="ophim/.../backdrop.jpg" />
            </label>
            <label class="text-sm font-bold text-slate-300">
              Overview
              <textarea v-model="movieForm.overview" class="admin-input min-h-28 py-3" />
            </label>
            <div class="grid grid-cols-3 gap-3">
              <label class="text-sm font-bold text-slate-300">
                Thể loại
                <select v-model="movieForm.genre_ids" class="admin-input min-h-28" multiple>
                  <option v-for="genre in lookups?.genres ?? []" :key="genre.id" :value="genre.id">{{ genre.name }}</option>
                </select>
              </label>
              <label class="text-sm font-bold text-slate-300">
                Quốc gia
                <select v-model="movieForm.country_ids" class="admin-input min-h-28" multiple>
                  <option v-for="country in lookups?.countries ?? []" :key="country.id" :value="country.id">{{ country.name }}</option>
                </select>
              </label>
              <label class="text-sm font-bold text-slate-300">
                Ngôn ngữ
                <select v-model="movieForm.language_ids" class="admin-input min-h-28" multiple>
                  <option v-for="language in lookups?.languages ?? []" :key="language.id" :value="language.id">{{ language.name }}</option>
                </select>
              </label>
            </div>
            <label class="inline-flex items-center gap-2 text-sm font-bold text-slate-300">
              <input v-model="movieForm.is_featured" type="checkbox" />
              Hiển thị nổi bật
            </label>
            <button class="inline-flex h-11 items-center justify-center gap-2 rounded-lg bg-[#ffe182] font-black text-[#11131d] hover:bg-[#ffd058]" type="submit" :disabled="isSaving || !canManageMovies">
              <Save :size="18" />
              {{ isSaving ? 'Đang lưu...' : editingMovie ? 'Lưu thay đổi' : 'Tạo phim' }}
            </button>
          </div>
        </form>

        <section class="rounded-2xl border border-white/8 bg-[#171922] p-5">
          <div class="flex flex-wrap items-center justify-between gap-3">
            <h2 class="text-lg font-black text-white">Danh sách phim</h2>
            <div class="flex flex-wrap gap-2">
              <label class="relative">
                <Search class="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500" :size="16" />
                <input v-model="searchQuery" class="h-10 rounded-lg border border-white/10 bg-white/6 pl-9 pr-3 text-sm outline-none focus:border-[#ffe182]" placeholder="Tìm phim..." />
              </label>
              <select v-model="statusFilter" class="h-10 rounded-lg border border-white/10 bg-white/6 px-3 text-sm outline-none focus:border-[#ffe182]" @change="loadAdminData">
                <option value="">Tất cả trạng thái</option>
                <option value="draft">Draft</option>
                <option value="published">Published</option>
                <option value="archived">Archived</option>
              </select>
              <button class="inline-flex h-10 items-center gap-2 rounded-lg bg-white/8 px-3 text-sm font-bold hover:bg-white/14" type="button" @click="loadAdminData">
                <RefreshCw :size="16" />
                Tải lại
              </button>
            </div>
          </div>

          <div class="mt-5 overflow-x-auto">
            <table class="w-full min-w-[760px] border-separate border-spacing-0 text-left text-sm">
              <thead class="text-xs uppercase tracking-wide text-slate-500">
                <tr>
                  <th class="border-b border-white/8 py-3 pr-4">Phim</th>
                  <th class="border-b border-white/8 py-3 pr-4">Loại</th>
                  <th class="border-b border-white/8 py-3 pr-4">Quyền</th>
                  <th class="border-b border-white/8 py-3 pr-4">Trạng thái</th>
                  <th class="border-b border-white/8 py-3 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="movie in filteredMovies" :key="movie.id" class="align-middle">
                  <td class="border-b border-white/6 py-3 pr-4">
                    <div class="flex items-center gap-3">
                      <img class="h-16 w-11 rounded-md object-cover ring-1 ring-white/10" :src="absoluteAssetUrl(movie.poster_path)" :alt="movie.title" />
                      <div class="min-w-0">
                        <p class="line-clamp-1 font-black text-white">{{ movie.title }}</p>
                        <p class="line-clamp-1 text-xs text-slate-400">{{ movie.slug }}</p>
                      </div>
                    </div>
                  </td>
                  <td class="border-b border-white/6 py-3 pr-4 text-slate-300">{{ movie.type }}</td>
                  <td class="border-b border-white/6 py-3 pr-4">
                    <span class="rounded-full bg-emerald-400/12 px-2 py-1 text-xs font-bold text-emerald-200">{{ movie.rights_status }}</span>
                  </td>
                  <td class="border-b border-white/6 py-3 pr-4">
                    <span class="rounded-full bg-white/8 px-2 py-1 text-xs font-bold text-slate-200">{{ movie.status }}</span>
                  </td>
                  <td class="border-b border-white/6 py-3 text-right">
                    <div class="inline-flex gap-1">
                      <button class="rounded-lg bg-white/8 p-2 text-slate-200 hover:bg-white/14" type="button" title="Sửa" @click="editMovie(movie)">
                        <Pencil :size="16" />
                      </button>
                      <button class="rounded-lg bg-emerald-400/12 p-2 text-emerald-200 hover:bg-emerald-400/20" type="button" title="Publish" @click="publishMovie(movie)">
                        <CheckCircle2 :size="16" />
                      </button>
                      <button class="rounded-lg bg-red-500/12 p-2 text-red-200 hover:bg-red-500/20" type="button" title="Xóa" @click="deleteMovie(movie)">
                        <Trash2 :size="16" />
                      </button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
            <div v-if="!filteredMovies.length" class="grid min-h-40 place-items-center text-sm font-bold text-slate-500">
              Chưa có phim phù hợp.
            </div>
          </div>
        </section>
      </div>
    </section>
  </main>
</template>
