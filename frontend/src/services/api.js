const API_BASE_URL = resolveApiBaseUrl()
const ADMIN_SESSION_KEY = 'zmovie_admin_session'

const posterFallback =
  'https://images.unsplash.com/photo-1608889825103-eb5ed706fc64?auto=format&fit=crop&w=520&q=85'
const backdropFallback =
  'https://images.unsplash.com/photo-1524985069026-dd778a71c7b4?auto=format&fit=crop&w=2200&q=85'
const videoFallback =
  'https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4'

async function request(path, params = {}) {
  const url = new URL(`${API_BASE_URL}${path}`)
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      url.searchParams.set(key, value)
    }
  })

  const headers = {
    Accept: 'application/json',
  }

  const token = currentAccessToken()

  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  const response = await fetch(url, { headers })

  if (!response.ok) {
    throw new Error(await errorMessage(response))
  }

  return response.json()
}

async function send(path, method, body) {
  const headers = {
    Accept: 'application/json',
    'Content-Type': 'application/json',
  }

  const token = currentAccessToken()

  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  })

  if (!response.ok) {
    throw new Error(await errorMessage(response))
  }

  if (response.status === 204) return null
  return response.json()
}

function currentAccessToken() {
  try {
    const session = JSON.parse(window.localStorage.getItem(ADMIN_SESSION_KEY) ?? 'null')
    return session?.accessToken ?? ''
  } catch {
    return ''
  }
}

async function errorMessage(response) {
  try {
    const payload = await response.json()
    const firstValidationError = payload.errors
      ? Object.values(payload.errors).flat().find(Boolean)
      : ''

    return firstValidationError || payload.message || `API ${response.status}: ${response.statusText}`
  } catch {
    return `API ${response.status}: ${response.statusText}`
  }
}

function resolveApiBaseUrl() {
  const configuredUrl = import.meta.env.VITE_API_BASE_URL

  if (configuredUrl) {
    return normalizeApiBaseUrl(configuredUrl)
  }

  if (typeof window !== 'undefined') {
    return normalizeApiBaseUrl(window.location.origin)
  }

  return ''
}

function normalizeApiBaseUrl(value) {
  const trimmedUrl = String(value).replace(/\/+$/, '')

  return trimmedUrl === '/' ? '' : trimmedUrl
}

export function absoluteAssetUrl(path) {
  if (!path) return ''
  if (/^https?:\/\//i.test(path)) return path

  const apiUrl = new URL(API_BASE_URL)
  const cleanPath = String(path).replace(/^\/+/, '')

  if (cleanPath.startsWith('storage/')) {
    return `${apiUrl.origin}/${cleanPath}`
  }

  return `${apiUrl.origin}/storage/${cleanPath}`
}

function playableUrl(path) {
  if (!path) return ''
  return absoluteAssetUrl(path)
}

function streamUrl(source) {
  if (!source?.url) return ''
  if (source.source_type === 'hls') return playableUrl(source.url)
  if (/^https?:\/\//i.test(source.url) || !source.id) return playableUrl(source.url)

  return `${API_BASE_URL}/video-sources/${source.id}/stream`
}

function preferredVideoSource(sources = []) {
  return (
    sources.find((source) => source.is_active && source.is_default) ??
    sources.find((source) => source.is_active) ??
    sources[0] ??
    null
  )
}

function movieCategory(movie) {
  if (movie.type === 'series') return 'Phim Bộ Mới'
  if (movie.type === 'short') return 'TV Show'
  return 'Phim Lẻ Mới'
}

export function normalizeMovie(movie) {
  const seasons = movie.seasons ?? []
  const firstSeason = seasons[0]
  const episodeCount = seasons.reduce(
    (total, season) => total + (season.episodes?.length ?? 0),
    0,
  )

  const videoSource = preferredVideoSource(movie.video_sources ?? movie.videoSources ?? [])
  const episodes = seasons
    .flatMap((season) => {
      return (season.episodes ?? []).map((episode) => {
        const episodeSource = preferredVideoSource(episode.video_sources ?? episode.videoSources ?? [])

        return {
          id: episode.id,
          slug: episode.slug,
          title: episode.title ?? `Tập ${episode.episode_number}`,
          number: episode.episode_number,
          seasonId: season.id,
          seasonNumber: season.season_number,
          seasonTitle: season.title,
          overview: episode.overview,
          runtimeMinutes: episode.runtime_minutes,
          still: absoluteAssetUrl(episode.still_path),
          status: episode.status,
          publishedAt: episode.published_at,
          videoUrl: streamUrl(episodeSource),
          videoType: episodeSource?.source_type ?? '',
        }
      })
    })
    .filter((episode) => episode.status !== 'archived')
    .sort((a, b) => (a.seasonNumber - b.seasonNumber) || (a.number - b.number))

  return {
    id: movie.id,
    slug: movie.slug,
    title: movie.title ?? 'Chưa có tên',
    original: movie.original_title ?? movie.title ?? 'Untitled',
    category: movieCategory(movie),
    year: String(movie.release_year ?? ''),
    meta:
      movie.type === 'series'
        ? episodeCount
          ? `Tập ${episodeCount}`
          : firstSeason
            ? `Phần ${firstSeason.season_number}`
            : 'Đang cập nhật'
        : movie.runtime_minutes
          ? `${movie.runtime_minutes} phút`
          : 'Full',
    badge: movie.languages?.some((language) => language.pivot?.kind === 'dubbed')
      ? 'T.Minh'
      : 'P.Đề',
    imdb: Number(movie.average_rating ?? 0).toFixed(1),
    genres: (movie.genres ?? []).map((genre) => genre.name),
    countries: (movie.countries ?? []).map((country) => country.name),
    poster: absoluteAssetUrl(movie.poster_path) || posterFallback,
    backdrop: absoluteAssetUrl(movie.backdrop_path) || backdropFallback,
    description:
      movie.overview ??
      'Nội dung đang được ZMovie cập nhật. Bạn có thể theo dõi phim này để nhận thông tin mới nhất.',
    isFeatured: Boolean(movie.is_featured),
    viewCount: movie.view_count ?? 0,
    episodes,
    videoUrl: streamUrl(videoSource) || playableUrl(movie.trailer_url ?? videoFallback),
    videoType: videoSource?.source_type ?? 'mp4',
    raw: movie,
  }
}

function normalizePaginatedMovies(payload) {
  return (payload.data ?? payload).map(normalizeMovie)
}

export async function fetchMovies(params = {}) {
  const payload = await request('/movies', {
    status: 'published',
    rights_status: 'cleared',
    per_page: 50,
    ...params,
  })

  return normalizePaginatedMovies(payload)
}

export async function searchMovies(params = {}) {
  const payload = await request('/search/movies', {
    per_page: 50,
    sort: 'latest',
    ...params,
  })

  return normalizePaginatedMovies(payload)
}

export async function fetchMovie(idOrSlug) {
  const payload = await request(`/movies/${idOrSlug}`)
  return normalizeMovie(payload)
}

export async function fetchLookups() {
  return request('/lookups')
}

export const adminApi = {
  login: (payload) => send('/auth/login', 'POST', payload),
  logout: () => send('/auth/logout', 'POST'),
  me: () => request('/auth/me'),
  demoAccounts: () => request('/auth/demo-accounts'),
  listMovies: (params) => request('/movies', { per_page: 100, ...params }),
  createMovie: (payload) => send('/movies', 'POST', payload),
  updateMovie: (id, payload) => send(`/movies/${id}`, 'PUT', payload),
  deleteMovie: (id) => send(`/movies/${id}`, 'DELETE'),
  publishMovie: (id) => send(`/movies/${id}/publish`, 'POST'),

  listContentProviders: (params) => request('/content-providers', params),
  createContentProvider: (payload) => send('/content-providers', 'POST', payload),
  updateContentProvider: (id, payload) => send(`/content-providers/${id}`, 'PUT', payload),
  deleteContentProvider: (id) => send(`/content-providers/${id}`, 'DELETE'),
  attachProviderMember: (id, payload) =>
    send(`/content-providers/${id}/members`, 'POST', payload),

  listContentLicenses: (params) => request('/content-licenses', params),
  createContentLicense: (payload) => send('/content-licenses', 'POST', payload),
  updateContentLicense: (id, payload) => send(`/content-licenses/${id}`, 'PUT', payload),
  deleteContentLicense: (id) => send(`/content-licenses/${id}`, 'DELETE'),
  approveContentLicense: (id) => send(`/content-licenses/${id}/approve`, 'POST'),

  listLegalDocuments: (params) => request('/legal-documents', params),
  createLegalDocument: (payload) => send('/legal-documents', 'POST', payload),
  updateLegalDocument: (id, payload) => send(`/legal-documents/${id}`, 'PUT', payload),

  listMovieUploads: (params) => request('/movie-uploads', params),
  createMovieUpload: (payload) => send('/movie-uploads', 'POST', payload),
  updateMovieUpload: (id, payload) => send(`/movie-uploads/${id}`, 'PUT', payload),
  deleteMovieUpload: (id) => send(`/movie-uploads/${id}`, 'DELETE'),
  submitMovieUpload: (id) => send(`/movie-uploads/${id}/submit`, 'POST'),
  approveMovieUpload: (id) => send(`/movie-uploads/${id}/approve`, 'POST'),

  listRoles: () => request('/roles'),
  listPermissions: () => request('/permissions'),
}
