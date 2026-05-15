<script setup>
import {
  ChevronDown,
  ChevronRight,
  Heart,
  Menu,
  Play,
  Search,
  Star,
  X,
} from 'lucide-vue-next'
import Hls from 'hls.js'
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AdminPanel from './AdminPanel.vue'
import logoUrl from './assets/zmovie-logo.png'
import logoMarkUrl from './assets/zmovie-mark.png'
import { fetchLookups, fetchMovie, fetchMovies, searchMovies } from './services/api'

const route = useRoute()
const router = useRouter()

const menuOpen = ref(false)
const searchQuery = ref('')
const activeHeroIndex = ref(0)
const currentView = ref('home')
const activeMovie = ref(null)
const apiError = ref('')
const backendStatus = ref('Đang kết nối backend')
const isLoading = ref(false)
const isSearching = ref(false)
const lookups = ref(null)
const catalogMovies = ref([])
const selectedCategory = ref('Tất cả')
const selectedTopic = ref(null)
const selectedEpisodeId = ref(null)
const videoRef = ref(null)
const heroStripRef = ref(null)
const heroHoverPaused = ref(false)
const heroDragPaused = ref(false)
const isHeroDragging = ref(false)
let hlsInstance = null
let heroAutoplayTimer = null
const heroDragState = {
  pointerId: null,
  startX: 0,
  startScrollLeft: 0,
  wasDragged: false,
}

const navItems = ['ZMovie', 'Phim Bộ', 'Phim Lẻ', 'TV Show', 'Phim Chiếu Rạp']
const filterItems = ['Thể Loại Phim', 'Quốc Gia', 'Năm']

const topics = [
  { slug: 'drama', title: 'Bền Bỉ: Chẳng Tiến...', query: 'Chính kịch', aliases: ['kịch', 'gia đình'], hint: 'Drama' },
  { slug: 'music', title: 'Âm Nhạc', query: 'Âm Nhạc', aliases: ['âm nhạc', 'sáng tạo'], hint: 'Music' },
  { slug: 'adventure', title: 'Ả Rập Xê Út', query: 'Phiêu Lưu', aliases: ['bí mật', 'kỳ bí'], hint: 'Adventure' },
  { slug: 'romance', title: 'Mưa Nhỏ Biết Vì', query: 'Tình Cảm', aliases: ['tình', 'hy sinh'], hint: 'Romance' },
  { slug: 'action', title: 'Tây Bắc Phong Vân', query: 'Hành Động', aliases: ['chiến', 'tốc độ', 'đua'], hint: 'Action' },
  { slug: 'thriller', title: 'Côn Đồ Thành Phố', query: 'Kinh Dị', aliases: ['nguyền', 'chết', 'hỗn loạn'], hint: 'Thriller' },
]

const topicTones = [
  'from-[#72809a] to-[#ba6475]',
  'from-[#9473c6] to-[#d36d7d]',
  'from-[#757fa3] to-[#bf6974]',
  'from-[#7b7aa0] to-[#c66c74]',
  'from-[#63b99e] to-[#cc7770]',
  'from-[#9473c6] to-[#d36d7d]',
]

const categoryRoutes = {
  'phim-bo': 'Phim Bộ Mới',
  'phim-le': 'Phim Lẻ Mới',
  'phim-chieu-rap': 'Phim Chiếu Rạp',
}

const categorySlugs = Object.fromEntries(
  Object.entries(categoryRoutes).map(([slug, category]) => [category, slug]),
)

const topicSlugs = {
  drama: 'Bền Bỉ: Chẳng Tiến...',
  music: 'Âm Nhạc',
  adventure: 'Ả Rập Xê Út',
  romance: 'Mưa Nhỏ Biết Vì',
  action: 'Tây Bắc Phong Vân',
  thriller: 'Côn Đồ Thành Phố',
  tvshow: 'TV Show',
}

const fallbackMovies = [
  {
    id: 'the-favor',
    title: 'Kẻ Cứu Rỗi',
    original: 'The Favor',
    category: 'Phim Lẻ Mới',
    year: '2025',
    meta: 'Full',
    badge: 'P.Đề',
    imdb: '0',
    genres: ['Chính kịch', 'Kinh Dị'],
    poster:
      'https://images.unsplash.com/photo-1608889825103-eb5ed706fc64?auto=format&fit=crop&w=520&q=85',
    backdrop:
      'https://images.unsplash.com/photo-1524985069026-dd778a71c7b4?auto=format&fit=crop&w=2200&q=85',
    description:
      'Phim Kẻ Cứu Rỗi là một tác phẩm điện ảnh Hàn Quốc đầy kịch tính, khám phá ranh giới mong manh giữa phép màu và lời nguyền trong cuộc sống gia đình.',
  },
  {
    id: 'enchanting-cranium',
    title: 'Liêu Trai: Mỹ Nhân Thủ',
    original: 'The Enchanting Cranium Mystery',
    category: 'Phim Lẻ Mới',
    year: '2025',
    meta: 'Full',
    badge: 'P.Đề',
    imdb: '7.1',
    genres: ['Cổ Trang', 'Bí Ẩn'],
    poster:
      'https://images.unsplash.com/photo-1526816229784-65d5d54ac8bc?auto=format&fit=crop&w=520&q=85',
    backdrop:
      'https://images.unsplash.com/photo-1534447677768-be436bb09401?auto=format&fit=crop&w=2200&q=85',
    description:
      'Một vụ án kỳ bí kéo những nhân vật trẻ vào vòng xoáy bí mật, tình yêu và những lời nguyền cổ xưa.',
  },
  {
    id: 'gabriels-inferno',
    title: 'Giáo Sư Gabriel: Phần 2',
    original: "Gabriel's Inferno: Part II",
    category: 'Phim Lẻ Mới',
    year: '2024',
    meta: 'Full',
    badge: 'P.Đề',
    imdb: '6.8',
    genres: ['Tình Cảm', 'Tâm Lý'],
    poster:
      'https://images.unsplash.com/photo-1516585427167-9f4af9627e6c?auto=format&fit=crop&w=520&q=85',
    backdrop:
      'https://images.unsplash.com/photo-1519741497674-611481863552?auto=format&fit=crop&w=2200&q=85',
    description:
      'Một chuyện tình nhiều tổn thương tiếp tục mở ra giữa những lựa chọn khó nói và quá khứ chưa khép lại.',
  },
  {
    id: 'in-the-name-father',
    title: 'Điệp Án Truy Tung',
    original: 'In The Name Of The Father',
    category: 'Phim Lẻ Mới',
    year: '2025',
    meta: 'Full',
    badge: 'P.Đề',
    imdb: '7.4',
    genres: ['Hành Động', 'Gia Đình'],
    poster:
      'https://images.unsplash.com/photo-1518709268805-4e9042af2176?auto=format&fit=crop&w=520&q=85',
    backdrop:
      'https://images.unsplash.com/photo-1485846234645-a62644f84728?auto=format&fit=crop&w=2200&q=85',
    description:
      'Một người cha bị cuốn vào cuộc truy tìm nguy hiểm khi sự thật về gia đình dần bị phơi bày.',
  },
  {
    id: 'the-gates',
    title: 'Khu Khép Kín',
    original: 'The Gates',
    category: 'Phim Lẻ Mới',
    year: '2025',
    meta: 'Full',
    badge: 'P.Đề',
    imdb: '6.5',
    genres: ['Kinh Dị', 'Giật Gân'],
    poster:
      'https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?auto=format&fit=crop&w=520&q=85',
    backdrop:
      'https://images.unsplash.com/photo-1518709911915-712d5fd04677?auto=format&fit=crop&w=2200&q=85',
    description:
      'Một khu nhà tưởng chừng bình yên che giấu những cánh cửa dẫn đến bí mật chết người.',
  },
  {
    id: 'filing-love',
    title: 'Thanh Tra Bí Mật',
    original: 'Filing for Love',
    category: 'Phim Bộ Mới',
    year: '2025',
    meta: 'Tập 8',
    badge: 'P.Đề',
    imdb: '7.0',
    genres: ['Hài', 'Tình Cảm'],
    poster:
      'https://images.unsplash.com/photo-1529139574466-a303027c1d8b?auto=format&fit=crop&w=520&q=85',
    backdrop:
      'https://images.unsplash.com/photo-1492684223066-81342ee5ff30?auto=format&fit=crop&w=2200&q=85',
    description:
      'Một cặp đôi bất đắc dĩ cùng điều tra những hồ sơ kỳ lạ, vừa phá án vừa học cách tin nhau.',
  },
  {
    id: 'we-are-trying',
    title: 'Cuộc Chiến Trong Chúng Ta',
    original: 'We Are All Trying Here',
    category: 'Phim Bộ Mới',
    year: '2025',
    meta: 'Tập 10',
    badge: 'P.Đề',
    imdb: '7.2',
    genres: ['Đời Thường', 'Tâm Lý'],
    poster:
      'https://images.unsplash.com/photo-1511882150382-421056c89033?auto=format&fit=crop&w=520&q=85',
    backdrop:
      'https://images.unsplash.com/photo-1529156069898-49953e39b3ac?auto=format&fit=crop&w=2200&q=85',
    description:
      'Những con người trẻ đối mặt với áp lực đô thị, tình bạn và những lựa chọn trưởng thành.',
  },
  {
    id: 'lillys-verschwinden',
    title: 'Lilly Mất Tích (Phần 1)',
    original: 'Lillys Verschwinden (Season 1)',
    category: 'Phim Bộ Mới',
    year: '2025',
    meta: 'Tập 6',
    badge: 'P.Đề',
    imdb: '7.3',
    genres: ['Bí Ẩn', 'Tội Phạm'],
    poster:
      'https://images.unsplash.com/photo-1497032205916-ac775f0649ae?auto=format&fit=crop&w=520&q=85',
    backdrop:
      'https://images.unsplash.com/photo-1500534314209-a25ddb2bd429?auto=format&fit=crop&w=2200&q=85',
    description:
      'Vụ mất tích của Lilly buộc cả thị trấn phải nhìn lại những bí mật đã bị chôn giấu nhiều năm.',
  },
  {
    id: 'funny-af',
    title: 'Funny AF With Kevin Hart',
    original: 'Funny AF with Kevin Hart',
    category: 'Phim Bộ Mới',
    year: '2025',
    meta: 'Tập 4',
    badge: 'P.Đề',
    imdb: '6.9',
    genres: ['Hài', 'TV Show'],
    poster:
      'https://images.unsplash.com/photo-1527224538127-2104bb71c51b?auto=format&fit=crop&w=520&q=85',
    backdrop:
      'https://images.unsplash.com/photo-1508973379184-7517410fb0bc?auto=format&fit=crop&w=2200&q=85',
    description:
      'Chuỗi sân khấu hài độc thoại với nhịp nhanh, nhiều khách mời và các câu chuyện đời thường sắc bén.',
  },
  {
    id: 'all-blue-sky',
    title: 'Xanh Ngắt Bầu Trời',
    original: 'All the Blue in the Sky',
    category: 'Phim Bộ Mới',
    year: '2025',
    meta: 'Tập 12',
    badge: 'P.Đề',
    imdb: '7.6',
    genres: ['Phiêu Lưu', 'Gia Đình'],
    poster:
      'https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?auto=format&fit=crop&w=520&q=85',
    backdrop:
      'https://images.unsplash.com/photo-1493246507139-91e8fad9978e?auto=format&fit=crop&w=2200&q=85',
    description:
      'Một hành trình đường dài mở ra những kết nối mới giữa các thành viên trong một gia đình rạn nứt.',
  },
  {
    id: 'embers-love',
    title: 'Tân Hoa Duyên',
    original: 'Embers of Love',
    category: 'Phim Chiếu Rạp',
    year: '2025',
    meta: 'Full',
    badge: 'P.Đề',
    imdb: '7.5',
    genres: ['Cổ Trang', 'Lãng Mạn'],
    poster:
      'https://images.unsplash.com/photo-1534447677768-be436bb09401?auto=format&fit=crop&w=520&q=85',
    backdrop:
      'https://images.unsplash.com/photo-1526816229784-65d5d54ac8bc?auto=format&fit=crop&w=2200&q=85',
    description:
      'Giữa chiến loạn, một mối tình cũ được thắp lại bằng những lựa chọn đầy hy sinh.',
  },
  {
    id: 'mf-ghost',
    title: 'MF GHOST (Phần 3)',
    original: 'MF GHOST (Season 3)',
    category: 'Phim Chiếu Rạp',
    year: '2025',
    meta: 'Tập 3',
    badge: 'P.Đề',
    imdb: '7.8',
    genres: ['Anime', 'Thể Thao'],
    poster:
      'https://images.unsplash.com/photo-1493238792000-8113da705763?auto=format&fit=crop&w=520&q=85',
    backdrop:
      'https://images.unsplash.com/photo-1503736334956-4c8f8e92946d?auto=format&fit=crop&w=2200&q=85',
    description:
      'Những tay đua trẻ bước vào mùa giải mới với tốc độ, chiến thuật và áp lực lớn hơn trước.',
  },
  {
    id: 'street-trash',
    title: 'Rác Đường Phố',
    original: 'Street Trash',
    category: 'Phim Chiếu Rạp',
    year: '2024',
    meta: 'Full',
    badge: 'P.Đề',
    imdb: '6.1',
    genres: ['Kinh Dị', 'Hài Đen'],
    poster:
      'https://images.unsplash.com/photo-1500534314209-a25ddb2bd429?auto=format&fit=crop&w=520&q=85',
    backdrop:
      'https://images.unsplash.com/photo-1519608487953-e999c86e7455?auto=format&fit=crop&w=2200&q=85',
    description:
      'Một đô thị hỗn loạn biến thành sân khấu cho những tình huống kinh dị quái dị và châm biếm.',
  },
  {
    id: 'sparklehorse',
    title: 'Tia Sáng Tuyệt Đẹp',
    original: 'This Is Sparklehorse',
    category: 'Phim Chiếu Rạp',
    year: '2024',
    meta: 'Full',
    badge: 'P.Đề',
    imdb: '7.7',
    genres: ['Tài Liệu', 'Âm Nhạc'],
    poster:
      'https://images.unsplash.com/photo-1516280440614-37939bbacd81?auto=format&fit=crop&w=520&q=85',
    backdrop:
      'https://images.unsplash.com/photo-1516280440614-37939bbacd81?auto=format&fit=crop&w=2200&q=85',
    description:
      'Một lát cắt tài liệu về âm nhạc, ký ức và những giai đoạn sáng tạo đầy biến động.',
  },
]

const sections = ['Phim Lẻ Mới', 'Phim Bộ Mới', 'Phim Chiếu Rạp']
const movies = ref(fallbackMovies)

const allMovies = computed(() => movies.value.length ? movies.value : fallbackMovies)
const featuredMovies = computed(() => {
  const featured = allMovies.value.filter((movie) => movie.isFeatured)
  return (featured.length ? featured : allMovies.value).slice(0, 7)
})

const activeHeroMovie = computed(() => {
  return featuredMovies.value[activeHeroIndex.value] ?? featuredMovies.value[0] ?? fallbackMovies[0]
})

const activeEpisodes = computed(() => activeMovie.value?.episodes ?? [])

const activeEpisode = computed(() => {
  if (!activeEpisodes.value.length) return null

  return (
    activeEpisodes.value.find((episode) => String(episode.id) === String(selectedEpisodeId.value)) ??
    activeEpisodes.value[0]
  )
})

const activeVideoUrl = computed(() => {
  return activeEpisode.value?.videoUrl || activeMovie.value?.videoUrl || ''
})

const isAdminRoute = computed(() => route.name === 'admin' || route.name === 'admin-login')

const filteredMovies = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()
  const topicTerms = selectedTopic.value
    ? [selectedTopic.value.query, ...(selectedTopic.value.aliases ?? [])]
        .filter(Boolean)
        .map((term) => term.toLowerCase())
    : []

  return allMovies.value.filter((movie) => {
    const categoryMatches =
      selectedCategory.value === 'Tất cả' ||
      (selectedCategory.value === 'Phim Chiếu Rạp'
        ? movie.category === 'Phim Chiếu Rạp' || movie.category === 'Phim Lẻ Mới'
        : movie.category === selectedCategory.value)

    const searchableText = [
      movie.title,
      movie.original,
      movie.year,
      movie.category,
      movie.description,
      ...(movie.genres ?? []),
      ...(movie.countries ?? []),
    ]
      .join(' ')
      .toLowerCase()

    const topicMatches =
      !topicTerms.length || topicTerms.some((term) => searchableText.includes(term))

    const queryMatches = !query || searchableText.includes(query)

    return categoryMatches && topicMatches && queryMatches
  })
})

const visibleSections = computed(() => {
  const sectionTitles =
    selectedCategory.value === 'Tất cả'
      ? sections
      : sections.filter((section) => {
          if (selectedCategory.value === 'Phim Chiếu Rạp') return section !== 'Phim Bộ Mới'
          return section === selectedCategory.value
        })

  return sectionTitles
    .map((title) => {
      const items = filteredMovies.value.filter((movie) => {
        const inSection = movie.category === title
        return inSection
      })
      return { title, items }
    })
    .filter((section) => section.items.length)
})

function movieRouteId(movie) {
  return movie.slug || movie.id
}

function findMovieByRouteId(id) {
  const normalizedId = String(id)
  return allMovies.value.find((movie) => {
    return String(movie.id) === normalizedId || movie.slug === normalizedId
  })
}

function ensureSelectedEpisode(movie = activeMovie.value) {
  const episodes = movie?.episodes ?? []

  if (!episodes.length) {
    selectedEpisodeId.value = null
    return
  }

  const selectedExists = episodes.some((episode) => String(episode.id) === String(selectedEpisodeId.value))

  if (!selectedExists) {
    selectedEpisodeId.value = episodes[0].id
  }
}

function topicBySlug(slug) {
  const topicTitle = topicSlugs[slug]
  if (topicTitle === 'TV Show') {
    return { slug: 'tvshow', title: 'TV Show', query: 'TV Show', aliases: ['show', 'sân khấu'], hint: 'Show' }
  }

  return topics.find((topic) => topic.slug === slug || topic.title === topicTitle) ?? null
}

async function applyRouteState() {
  menuOpen.value = false

  if (route.name === 'movie-detail' || route.name === 'watch') {
    const movie = findMovieByRouteId(route.params.id)
    if (movie) {
      activeMovie.value = movie
      ensureSelectedEpisode(movie)
      currentView.value = route.name === 'watch' ? 'watch' : 'detail'
      selectedCategory.value = 'Tất cả'
      selectedTopic.value = null

      if (movie.id && typeof movie.id !== 'string') {
        try {
          const freshMovie = await fetchMovie(movie.id)
          activeMovie.value = freshMovie
          ensureSelectedEpisode(freshMovie)
        } catch (error) {
          apiError.value = error.message
        }
      }
      return
    }
  }

  currentView.value = 'home'
  activeMovie.value = null
  selectedEpisodeId.value = null

  if (route.name === 'category') {
    selectedCategory.value = categoryRoutes[route.params.slug] ?? 'Tất cả'
    selectedTopic.value = null
    searchQuery.value = ''
    return
  }

  if (route.name === 'topic') {
    selectedCategory.value = 'Tất cả'
    selectedTopic.value = topicBySlug(route.params.slug)
    searchQuery.value = ''
    return
  }

  if (route.name === 'search') {
    selectedCategory.value = 'Tất cả'
    selectedTopic.value = null
    searchQuery.value = String(route.query.q ?? '')
    return
  }

  selectedCategory.value = 'Tất cả'
  selectedTopic.value = null
  searchQuery.value = ''
}

async function loadInitialData() {
  isLoading.value = true
  apiError.value = ''

  try {
    const [movieData, lookupData] = await Promise.all([
      fetchMovies({ sort: 'latest' }),
      fetchLookups().catch(() => null),
    ])

    movies.value = movieData.length ? movieData : fallbackMovies
    catalogMovies.value = movieData
    lookups.value = lookupData
    backendStatus.value = movieData.length
      ? `Đã đồng bộ ${movieData.length} phim từ backend`
      : 'Backend chưa có phim published, đang dùng dữ liệu demo'
  } catch (error) {
    movies.value = fallbackMovies
    apiError.value = error.message
    backendStatus.value = 'Không kết nối được backend, đang dùng dữ liệu demo'
  } finally {
    isLoading.value = false
    await applyRouteState()
  }
}

async function runSearch(query) {
  const trimmed = query.trim()

  if (!trimmed) {
    await loadInitialData()
    return
  }

  isSearching.value = true

  try {
    const results = await searchMovies({ q: trimmed })
    movies.value = results
    backendStatus.value = `Tìm thấy ${results.length} kết quả từ backend`
    apiError.value = ''
  } catch (error) {
    apiError.value = error.message
    backendStatus.value = 'Search API lỗi, đang lọc cục bộ'
    movies.value = catalogMovies.value.length ? catalogMovies.value : fallbackMovies
  } finally {
    isSearching.value = false
  }
}

async function openMovie(movie) {
  await router.push({ name: 'movie-detail', params: { id: movieRouteId(movie) } })
}

function openPlayer(movie, episode = null) {
  if (episode) {
    selectedEpisodeId.value = episode.id
  }

  router.push({ name: 'watch', params: { id: movieRouteId(movie) } })
}

function showHome() {
  router.push({ name: 'home' })
}

function selectCategory(category) {
  const slug = categorySlugs[category]
  router.push(slug ? { name: 'category', params: { slug } } : { name: 'home' })
}

function selectTopic(topic) {
  router.push({ name: 'topic', params: { slug: topic.slug } })
}

function clearFilters() {
  router.push({ name: 'home' })
}

function selectHero(index) {
  activeHeroIndex.value = index
}

function selectEpisode(episode, shouldPlay = false) {
  selectedEpisodeId.value = episode.id

  if (shouldPlay && activeMovie.value) {
    openPlayer(activeMovie.value, episode)
  }
}

function advanceHero() {
  const total = featuredMovies.value.length
  if (total <= 1) return

  activeHeroIndex.value = (activeHeroIndex.value + 1) % total
}

function startHeroAutoplay() {
  stopHeroAutoplay()
  heroAutoplayTimer = window.setInterval(() => {
    if (currentView.value === 'home' && !heroHoverPaused.value && !heroDragPaused.value) {
      advanceHero()
    }
  }, 4500)
}

function stopHeroAutoplay() {
  if (!heroAutoplayTimer) return

  window.clearInterval(heroAutoplayTimer)
  heroAutoplayTimer = null
}

function pauseHeroAutoplay() {
  heroHoverPaused.value = true
}

function resumeHeroAutoplay() {
  heroHoverPaused.value = false
}

function startHeroDrag(event) {
  if (event.button !== undefined && event.button !== 0) return

  const strip = heroStripRef.value
  if (!strip) return

  heroDragState.pointerId = event.pointerId
  heroDragState.startX = event.clientX
  heroDragState.startScrollLeft = strip.scrollLeft
  heroDragState.wasDragged = false
  heroDragPaused.value = true
  isHeroDragging.value = true
  strip.setPointerCapture?.(event.pointerId)
}

function moveHeroDrag(event) {
  if (!isHeroDragging.value || heroDragState.pointerId !== event.pointerId) return

  const strip = heroStripRef.value
  if (!strip) return

  const deltaX = event.clientX - heroDragState.startX

  if (Math.abs(deltaX) > 4) {
    heroDragState.wasDragged = true
    event.preventDefault()
  }

  strip.scrollLeft = heroDragState.startScrollLeft - deltaX
}

function stopHeroDrag(event) {
  if (heroDragState.pointerId !== event.pointerId) return

  heroStripRef.value?.releasePointerCapture?.(event.pointerId)
  heroDragState.pointerId = null
  heroDragPaused.value = false
  isHeroDragging.value = false
}

function handleHeroThumbnailClick(index) {
  if (heroDragState.wasDragged) {
    window.setTimeout(() => {
      heroDragState.wasDragged = false
    }, 0)
    return
  }

  selectHero(index)
}

function playActiveHero() {
  openPlayer(activeHeroMovie.value)
}

function scrollToTop() {
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

function useFallbackImage(event) {
  const image = event.currentTarget
  if (!image || image.dataset.fallbackApplied) return

  image.dataset.fallbackApplied = 'true'
  image.src = logoMarkUrl
  image.classList.add('object-contain', 'p-3')
}

function setupPlayerSource() {
  const video = videoRef.value
  const url = activeVideoUrl.value

  if (!video || !url) return

  if (hlsInstance) {
    hlsInstance.destroy()
    hlsInstance = null
  }

  video.pause()
  video.removeAttribute('src')
  video.load()

  if (url.includes('.m3u8') && video.canPlayType('application/vnd.apple.mpegurl')) {
    video.src = url
    video.load()
    return
  }

  if (url.includes('.m3u8') && Hls.isSupported()) {
    hlsInstance = new Hls()
    hlsInstance.loadSource(url)
    hlsInstance.attachMedia(video)
    return
  }

  video.src = url
  video.load()
}

let searchDebounce
watch(searchQuery, (query) => {
  window.clearTimeout(searchDebounce)
  searchDebounce = window.setTimeout(() => {
    const trimmed = query.trim()
    if (trimmed) {
      if (route.name !== 'search' || route.query.q !== trimmed) {
        router.replace({ name: 'search', query: { q: trimmed } })
      }
    } else if (route.name === 'search') {
      router.replace({ name: 'home' })
    }
    runSearch(query)
  }, 350)
})

watch(
  () => route.fullPath,
  () => {
    applyRouteState()
  },
)

watch(
  () => [currentView.value, activeVideoUrl.value],
  async () => {
    if (currentView.value === 'watch') {
      await nextTick()
      setupPlayerSource()
    } else if (hlsInstance) {
      hlsInstance.destroy()
      hlsInstance = null
    }
  },
)

watch(featuredMovies, () => {
  if (activeHeroIndex.value >= featuredMovies.value.length) {
    activeHeroIndex.value = 0
  }
  startHeroAutoplay()
})

onMounted(async () => {
  await loadInitialData()
  startHeroAutoplay()
})

onBeforeUnmount(() => {
  stopHeroAutoplay()

  if (hlsInstance) {
    hlsInstance.destroy()
  }
})
</script>

<template>
  <AdminPanel v-if="isAdminRoute" />
  <div v-else class="min-h-screen overflow-x-hidden bg-[#11131d] text-slate-50">
    <header
      class="sticky top-0 z-20 grid min-h-[72px] grid-cols-[minmax(0,1fr)_44px] items-center gap-4 border-b border-white/7 bg-[#090a12]/94 px-4 py-3 backdrop-blur-[18px] md:grid-cols-[auto_minmax(240px,420px)_auto] md:gap-5 md:px-[clamp(20px,4vw,52px)] md:py-0 xl:grid-cols-[auto_minmax(250px,420px)_1fr]"
    >
      <button
        class="flex min-w-0 items-center gap-2.5 text-left"
        type="button"
        aria-label="ZMovie home"
        @click="showHome"
      >
        <img class="h-11 w-11 rounded-full object-contain" :src="logoMarkUrl" alt="" />
        <span>
          <strong class="block text-[22px] leading-none text-white">ZMovie</strong>
          <small class="mt-0.5 block text-xs text-slate-400">Stream cinema</small>
        </span>
      </button>

      <div
        class="order-3 col-span-full flex h-10 min-w-0 items-center gap-3 rounded-lg border border-white/6 bg-[#20232d] px-4 text-slate-300 transition focus-within:border-[#ffe182]/70 md:order-none md:col-span-1 md:h-[46px] md:px-[18px]"
      >
        <Search :size="20" />
        <input
          v-model="searchQuery"
          class="w-full border-0 bg-transparent text-[15px] text-slate-50 outline-none placeholder:text-slate-200"
          type="search"
          placeholder="Tìm kiếm phim, diễn viên"
          @focus="currentView = 'home'"
        />
        <button
          v-if="searchQuery"
          class="grid h-7 w-7 shrink-0 place-items-center rounded-full bg-white/8 text-slate-300 transition hover:bg-white/14 hover:text-white"
          type="button"
          aria-label="Xóa tìm kiếm"
          @click="searchQuery = ''"
        >
          <X :size="15" />
        </button>
      </div>

      <button
        class="grid h-11 w-11 cursor-pointer place-items-center justify-self-end rounded-lg border border-white/10 bg-[#20232d] text-white xl:hidden"
        type="button"
        aria-label="Mở menu"
        @click="menuOpen = !menuOpen"
      >
        <X v-if="menuOpen" :size="22" />
        <Menu v-else :size="22" />
      </button>

      <nav
        :class="[
          'absolute left-5 right-5 top-32 hidden rounded-xl border border-white/8 bg-[#10121c]/98 p-4 text-sm font-bold text-[#f5f7fb] shadow-[0_22px_60px_rgba(0,0,0,0.45)] md:top-[74px] xl:static xl:flex xl:items-center xl:justify-end xl:gap-[clamp(14px,1.6vw,28px)] xl:border-0 xl:bg-transparent xl:p-0 xl:shadow-none',
          menuOpen ? 'grid gap-1 md:grid-cols-2 xl:flex' : 'xl:flex',
        ]"
      >
        <button
          v-for="item in navItems"
          :key="item"
          :class="[
            'inline-flex min-h-10 items-center gap-1 px-2.5 text-left whitespace-nowrap transition-colors hover:text-[#ffe182] xl:px-0',
            (item === 'ZMovie' && selectedCategory === 'Tất cả') || item === selectedCategory
              ? 'text-[#ffe182]'
              : '',
          ]"
          type="button"
          @click="
            item === 'ZMovie'
              ? showHome()
              : item === 'TV Show'
                ? selectTopic({ slug: 'tvshow', title: 'TV Show', query: 'TV Show', hint: 'Show' })
                : selectCategory(item === 'Phim Lẻ' ? 'Phim Lẻ Mới' : item)
          "
        >
          {{ item }}
        </button>
        <button
          v-for="item in filterItems"
          :key="item"
          class="inline-flex min-h-10 items-center gap-1 px-2.5 text-left whitespace-nowrap transition-colors hover:text-[#ffe182] xl:px-0"
          type="button"
          @click="selectCategory('Tất cả')"
        >
          {{ item }}
          <ChevronDown :size="14" />
        </button>
      </nav>
    </header>

    <main v-if="currentView === 'home'">
      <section
        class="hero-bg relative min-h-[540px] px-[18px] md:min-h-[500px] md:px-[clamp(20px,7vw,140px)] 2xl:px-[clamp(20px,14vw,370px)]"
        :style="{ backgroundImage: `linear-gradient(90deg, #131722 0%, rgba(19,23,34,.88) 26%, rgba(19,23,34,.32) 58%, #131722 100%), linear-gradient(180deg, rgba(19,23,34,0) 48%, #11131d 100%), url(${activeHeroMovie.backdrop})` }"
        @mouseenter="pauseHeroAutoplay"
        @mouseleave="resumeHeroAutoplay"
        @focusin="pauseHeroAutoplay"
        @focusout="resumeHeroAutoplay"
      >
        <div class="relative z-[1] max-w-xl pt-12 md:pt-16">
          <h1 class="mb-2 text-[clamp(30px,3vw,48px)] leading-tight font-black text-white">{{ activeHeroMovie.title }}</h1>
          <p class="mb-3.5 text-base font-bold text-[#ffe182]">{{ activeHeroMovie.original }}</p>
          <div class="flex max-w-80 flex-wrap gap-2.5">
            <span class="tag border-[#ffe182] text-[#ffe182]">IMDb {{ activeHeroMovie.imdb }}</span>
            <span class="tag">{{ activeHeroMovie.year }}</span>
            <span class="tag">{{ activeHeroMovie.meta }}</span>
            <span v-for="genre in activeHeroMovie.genres" :key="genre" class="tag">
              {{ genre }}
            </span>
          </div>
          <p class="my-6 line-clamp-3 max-w-[520px] text-sm font-semibold leading-[1.65] text-slate-100 md:my-7 md:text-[15px]">
            {{ activeHeroMovie.description }}
          </p>
          <div class="flex flex-wrap items-center gap-3">
            <button
              class="inline-flex h-12 cursor-pointer items-center gap-2 rounded-lg border-0 bg-linear-to-br from-[#ffe58f] to-[#ffd058] px-5 text-sm font-black text-[#11131d] shadow-[0_18px_48px_rgba(255,208,88,0.24)] transition hover:-translate-y-0.5 hover:shadow-[0_22px_56px_rgba(255,208,88,0.32)]"
              type="button"
              @click="playActiveHero"
            >
              <Play :size="18" fill="currentColor" />
              Xem phim
            </button>
            <button
              class="inline-flex h-12 cursor-pointer items-center gap-2 rounded-lg border border-white/14 bg-white/9 px-5 text-sm font-bold text-white transition hover:border-[#ffe182] hover:text-[#ffe182]"
              type="button"
              @click="openMovie(activeHeroMovie)"
            >
              Chi tiết
              <ChevronRight :size="16" />
            </button>
          </div>
        </div>

        <div
          ref="heroStripRef"
          :class="[
            'absolute bottom-8 left-[18px] right-[18px] z-[1] flex touch-pan-x select-none gap-3 overflow-x-auto scroll-smooth md:bottom-12 md:left-auto md:right-[clamp(20px,7vw,140px)] 2xl:right-[clamp(24px,14vw,360px)]',
            isHeroDragging ? 'cursor-grabbing scroll-auto' : 'cursor-grab',
          ]"
          aria-label="Phim nổi bật"
          @pointerdown="startHeroDrag"
          @pointermove="moveHeroDrag"
          @pointerup="stopHeroDrag"
          @pointercancel="stopHeroDrag"
          @pointerleave="stopHeroDrag"
        >
          <button
            v-for="(movie, index) in featuredMovies"
            :key="movie.id"
            :class="[
              'group relative h-16 w-12 shrink-0 cursor-pointer overflow-hidden rounded-[5px] border-2 bg-[#20232d] p-0 transition hover:border-white',
              index === activeHeroIndex ? 'border-white' : 'border-transparent',
            ]"
            type="button"
            @click="handleHeroThumbnailClick(index)"
          >
            <img class="h-full w-full object-cover" :src="movie.poster" :alt="movie.title" @error="useFallbackImage" />
            <span
              :class="[
                'absolute bottom-0 left-0 h-0.5 bg-[#ffe182] transition-all duration-[4500ms] group-hover:bg-white',
                index === activeHeroIndex ? 'w-full' : 'w-0',
              ]"
            ></span>
          </button>
        </div>
      </section>

      <section
        id="catalog"
        class="mx-auto max-w-[1810px] px-[18px] py-12 md:px-[clamp(20px,7vw,140px)] md:py-14 2xl:px-[clamp(20px,14vw,370px)]"
      >
        <h2 class="relative z-[2] max-w-[980px] text-2xl leading-tight font-extrabold text-slate-50 md:text-[clamp(24px,2vw,34px)]">
          ZMovie - Kho Phim Full HD - Xem Phim Online Vietsub, Thuyết Minh
        </h2>
        <div class="relative z-[2] mt-4 flex flex-wrap items-center gap-3 text-xs font-semibold">
          <span
            :class="[
              'rounded-full px-3 py-1.5',
              apiError ? 'bg-amber-400/12 text-amber-200' : 'bg-emerald-400/12 text-emerald-200',
            ]"
          >
            {{ isLoading ? 'Đang tải dữ liệu...' : backendStatus }}
          </span>
          <span v-if="isSearching" class="rounded-full bg-white/8 px-3 py-1.5 text-slate-300">
            Đang tìm kiếm...
          </span>
          <span v-if="lookups?.genres?.length" class="rounded-full bg-white/8 px-3 py-1.5 text-slate-300">
            {{ lookups.genres.length }} thể loại
          </span>
          <button
            v-if="selectedCategory !== 'Tất cả' || selectedTopic || searchQuery"
            class="rounded-full bg-[#ffe182] px-3 py-1.5 text-[#11131d] transition hover:bg-[#ffd058]"
            type="button"
            @click="clearFilters"
          >
            Xóa bộ lọc
          </button>
        </div>
        <div
          v-if="selectedCategory !== 'Tất cả' || selectedTopic"
          class="relative z-[2] mt-4 text-sm font-semibold text-slate-300"
        >
          Đang xem:
          <span class="text-[#ffe182]">
            {{ selectedTopic ? `Chủ đề ${selectedTopic.title}` : selectedCategory }}
          </span>
        </div>

        <div class="relative z-[2] mt-7 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6">
          <RouterLink
            v-for="(topic, index) in topics"
            :key="topic.title"
            :to="{ name: 'topic', params: { slug: topic.slug } }"
            :class="[
              'flex min-h-[118px] cursor-pointer flex-col justify-between overflow-hidden rounded-lg bg-linear-to-br p-4.5 text-left text-white shadow-[0_16px_42px_rgba(0,0,0,0.2)] transition hover:-translate-y-0.5 hover:brightness-105 md:min-h-[134px] md:p-5',
              topicTones[index],
              selectedTopic?.title === topic.title ? 'ring-2 ring-white' : '',
            ]"
          >
            <strong class="max-w-[170px] text-[clamp(20px,1.2vw,26px)] leading-tight">
              {{ topic.title }}
            </strong>
            <span class="inline-flex items-center gap-1.5 text-sm font-bold">
              {{ topic.hint }}
              <ChevronRight :size="15" />
            </span>
          </RouterLink>
        </div>

        <section
          class="mt-12 rounded-2xl border border-white/6 bg-linear-to-b from-[#252938]/90 to-[#11131d]/88 px-4 py-6 shadow-[0_24px_90px_rgba(0,0,0,0.22)] md:px-[clamp(24px,4vw,72px)] md:py-9"
          aria-labelledby="top-title"
        >
          <div class="flex items-center gap-2.5 text-[#ffe182]">
            <Star :size="20" fill="currentColor" />
            <h2 id="top-title" class="text-[clamp(21px,1.5vw,28px)] font-black text-white">
              TOP PHIM XEM NHIỀU
            </h2>
          </div>

          <div class="mt-7 grid grid-cols-2 gap-x-4 gap-y-7 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-8 xl:gap-x-5">
            <article v-for="(movie, index) in filteredMovies.slice(0, 8)" :key="movie.id" class="group min-w-0">
              <button
                :class="[
                  'relative block aspect-2/3 w-full overflow-hidden rounded-lg bg-[#252938] text-left shadow-[0_14px_38px_rgba(0,0,0,0.26)] transition group-hover:-translate-y-1 group-hover:shadow-[0_18px_52px_rgba(0,0,0,0.42)]',
                  index === 7 ? 'ring-3 ring-[#ffe182]' : 'ring-1 ring-white/8',
                ]"
                type="button"
                @click="openMovie(movie)"
              >
                <img class="h-full w-full object-cover" :src="movie.poster" :alt="movie.title" @error="useFallbackImage" />
                <span class="absolute right-2 bottom-2 max-w-[calc(100%-16px)] overflow-hidden rounded-md bg-[#303543]/92 px-2 py-1 text-[11px] font-black text-ellipsis whitespace-nowrap text-white shadow-lg backdrop-blur">
                  {{ movie.badge }} {{ movie.meta }}
                </span>
              </button>
              <div class="mt-3 grid grid-cols-[38px_minmax(0,1fr)] items-start gap-2">
                <span class="pt-0.5 text-[38px] leading-none font-black text-[#ffe182] italic md:text-[42px]">
                  {{ index + 1 }}
                </span>
                <div class="min-w-0">
                  <h3 class="line-clamp-2 min-h-[38px] text-sm leading-[1.35] font-extrabold text-white">
                    {{ movie.title }}
                  </h3>
                  <p class="mt-1 overflow-hidden text-xs leading-relaxed text-ellipsis whitespace-nowrap text-slate-400">
                    {{ movie.original }}
                  </p>
                  <small class="block overflow-hidden text-xs leading-relaxed text-ellipsis whitespace-nowrap text-slate-400">
                    {{ movie.year }} <span class="mx-1.5 text-slate-500">•</span> {{ movie.meta }}
                  </small>
                </div>
                <button
                  class="col-span-2 mt-1 inline-flex h-9 w-full items-center justify-center gap-1.5 rounded-lg border border-white/8 bg-white/8 px-3 text-xs font-black whitespace-nowrap text-slate-100 transition hover:border-[#ffe182] hover:bg-[#ffe182] hover:text-[#11131d]"
                  type="button"
                  @click="openPlayer(movie)"
                >
                  <Play :size="13" fill="currentColor" />
                  Xem ngay
                </button>
              </div>
            </article>
          </div>
        </section>

        <section
          v-for="section in visibleSections"
          :key="section.title"
          class="mt-14"
          :aria-labelledby="section.title"
        >
          <div class="mb-6 flex items-center gap-3.5">
            <h2 :id="section.title" class="text-[clamp(22px,1.6vw,30px)] font-black text-white">
              {{ section.title.toUpperCase() }}
            </h2>
            <button
              class="grid h-9 w-9 cursor-pointer place-items-center rounded-full border border-white/16 bg-white/8 text-white transition hover:border-[#ffe182] hover:text-[#ffe182]"
              type="button"
              aria-label="Xem thêm"
              @click="selectCategory(section.title)"
            >
              <ChevronRight :size="20" />
            </button>
          </div>

          <div class="grid grid-cols-2 gap-x-4 gap-y-8 md:grid-cols-4 xl:grid-cols-8">
            <article v-for="movie in section.items" :key="movie.id" class="min-w-0">
              <button
                class="group relative block aspect-2/3 w-full overflow-hidden rounded-lg bg-[#252938] text-left"
                type="button"
                @click="openPlayer(movie)"
              >
                <img class="h-full w-full object-cover transition duration-300 group-hover:scale-[1.04]" :src="movie.poster" :alt="movie.title" @error="useFallbackImage" />
                <span class="absolute right-2 bottom-2 rounded-[5px] bg-[#3f4454]/92 px-2 py-1 text-[11px] font-black text-white">
                  {{ movie.badge }}
                </span>
                <span class="absolute inset-0 grid place-items-center bg-black/0 opacity-0 transition group-hover:bg-black/35 group-hover:opacity-100">
                  <span class="grid h-12 w-12 place-items-center rounded-full bg-[#ffe182] text-[#11131d]">
                    <Play :size="22" fill="currentColor" />
                  </span>
                </span>
              </button>
              <h3 class="mt-3 overflow-hidden text-center text-sm font-bold text-ellipsis whitespace-nowrap text-white">
                {{ movie.title }}
              </h3>
              <p class="mt-1 overflow-hidden text-center text-xs text-ellipsis whitespace-nowrap text-slate-400">
                {{ movie.original }}
              </p>
              <button
                class="mx-auto mt-2 flex h-7 items-center gap-1 rounded-lg bg-white/8 px-3 text-xs font-bold text-slate-200 transition hover:bg-[#ffe182] hover:text-[#11131d]"
                type="button"
                @click="openMovie(movie)"
              >
                Chi tiết
                <ChevronRight :size="13" />
              </button>
            </article>
          </div>
        </section>

        <section
          v-if="searchQuery && !isSearching && !visibleSections.length"
          class="mt-14 rounded-xl border border-white/8 bg-white/5 px-6 py-10 text-center"
        >
          <h2 class="text-xl font-black text-white">Không tìm thấy phim phù hợp</h2>
          <p class="mt-2 text-sm text-slate-400">
            Thử tìm bằng tên phim, năm phát hành hoặc thể loại khác.
          </p>
          <button
            class="mt-5 rounded-full bg-[#ffe182] px-5 py-2 text-sm font-black text-[#11131d] transition hover:bg-[#ffd058]"
            type="button"
            @click="searchQuery = ''"
          >
            Xóa tìm kiếm
          </button>
        </section>
      </section>
    </main>

    <main v-else-if="currentView === 'watch' && activeMovie" class="bg-[#090a13] pb-20">
      <section class="mx-auto max-w-[1600px] px-4 py-6 md:px-8">
        <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div class="min-w-0">
            <p class="text-xs font-bold uppercase tracking-[0.18em] text-[#ffe182]">Đang xem</p>
            <h1 class="mt-1 truncate text-2xl font-black text-white md:text-3xl">
              {{ activeMovie.title }}
            </h1>
            <p class="mt-1 truncate text-sm font-semibold text-slate-400">
              {{ activeMovie.original }} · {{ activeMovie.year }} · {{ activeEpisode?.title ?? activeMovie.meta }}
            </p>
          </div>

          <div class="flex gap-2">
            <button
              class="rounded-full border border-white/16 bg-white/8 px-4 py-2 text-sm font-bold text-white transition hover:border-[#ffe182] hover:text-[#ffe182]"
              type="button"
              @click="openMovie(activeMovie)"
            >
              Chi tiết
            </button>
            <button
              class="rounded-full bg-[#ffe182] px-4 py-2 text-sm font-black text-[#11131d] transition hover:bg-[#ffd058]"
              type="button"
              @click="showHome"
            >
              Danh sách phim
            </button>
          </div>
        </div>

        <video
          ref="videoRef"
          class="aspect-video w-full rounded-2xl bg-black shadow-[0_24px_90px_rgba(0,0,0,0.45)]"
          :key="`${activeMovie.id}-${activeEpisode?.id ?? 'movie'}`"
          :poster="activeEpisode?.still || activeMovie.backdrop"
          controls
          autoplay
          playsinline
        ></video>

        <div class="mt-6 grid gap-6 lg:grid-cols-[1fr_360px]">
          <section class="rounded-2xl border border-white/8 bg-white/5 p-5">
            <div class="flex flex-wrap gap-2">
              <span class="tag border-[#ffe182] text-[#ffe182]">IMDb {{ activeMovie.imdb }}</span>
              <span class="tag">{{ activeMovie.year }}</span>
              <span class="tag">{{ activeEpisode?.title ?? activeMovie.meta }}</span>
              <span v-for="genre in activeMovie.genres" :key="genre" class="tag">
                {{ genre }}
              </span>
            </div>
            <p class="mt-5 line-clamp-4 text-sm leading-7 text-slate-300 md:text-[15px]">
              {{ activeEpisode?.overview || activeMovie.description }}
            </p>

            <div v-if="activeEpisodes.length" class="mt-6">
              <div class="mb-3 flex items-center justify-between gap-3">
                <h2 class="text-lg font-black text-white">Danh sách tập</h2>
                <span class="text-xs font-bold text-slate-400">
                  {{ activeEpisodes.length }} tập
                </span>
              </div>
              <div class="grid grid-cols-3 gap-2 sm:grid-cols-4 md:grid-cols-6 xl:grid-cols-8">
                <button
                  v-for="episode in activeEpisodes"
                  :key="episode.id"
                  :class="[
                    'h-10 rounded-lg border px-3 text-sm font-black transition',
                    activeEpisode?.id === episode.id
                      ? 'border-[#ffe182] bg-[#ffe182] text-[#11131d]'
                      : 'border-white/10 bg-white/7 text-slate-100 hover:border-[#ffe182] hover:text-[#ffe182]',
                  ]"
                  type="button"
                  @click="selectEpisode(episode)"
                >
                  Tập {{ episode.number }}
                </button>
              </div>
            </div>
          </section>

          <aside class="rounded-2xl border border-white/8 bg-white/5 p-5">
            <h2 class="text-lg font-black text-white">Phim nổi bật</h2>
            <div class="mt-4 grid grid-cols-3 gap-3">
              <button
                v-for="movie in featuredMovies.slice(0, 6)"
                :key="movie.id"
                :class="[
                  'overflow-hidden rounded-lg border bg-[#20232d] p-0 transition hover:border-[#ffe182]',
                  movie.id === activeMovie.id ? 'border-[#ffe182]' : 'border-white/10',
                ]"
                type="button"
                @click="openPlayer(movie)"
              >
                <img class="aspect-2/3 w-full object-cover" :src="movie.poster" :alt="movie.title" @error="useFallbackImage" />
              </button>
            </div>
          </aside>
        </div>
      </section>
    </main>

    <main v-else-if="activeMovie" class="pb-20">
      <section
        class="relative mx-auto mt-2 min-h-[500px] max-w-[1800px] overflow-hidden rounded-none px-5 py-12 md:mt-2 md:min-h-[500px] md:rounded-2xl md:px-12 md:py-14"
        :style="{ backgroundImage: `linear-gradient(90deg, rgba(35,39,56,.98) 0%, rgba(35,39,56,.9) 28%, rgba(35,39,56,.15) 63%, rgba(35,39,56,.75) 100%), linear-gradient(180deg, rgba(35,39,56,0) 64%, #171922 100%), url(${activeMovie.backdrop})`, backgroundSize: 'cover', backgroundPosition: 'center' }"
      >
        <div class="relative z-[1] max-w-xl">
          <h1 class="text-3xl font-black text-white">{{ activeMovie.title }}</h1>
          <p class="mt-2 font-bold text-[#ffe182]">{{ activeMovie.original }}</p>
          <div class="mt-4 flex flex-wrap gap-2">
            <span class="tag border-[#ffe182] text-[#ffe182]">IMDb {{ activeMovie.imdb }}</span>
            <span class="tag">{{ activeMovie.year }}</span>
            <span class="tag">{{ activeMovie.meta }}</span>
            <span v-for="genre in activeMovie.genres" :key="genre" class="tag">
              {{ genre }}
            </span>
          </div>
          <p class="mt-7 line-clamp-4 text-sm leading-7 font-semibold text-white md:text-[15px]">
            {{ activeMovie.description }}
          </p>
          <button
            class="mt-9 grid h-[72px] w-[72px] cursor-pointer place-items-center rounded-full bg-linear-to-br from-[#ffe58f] to-[#ffd058] text-[#11131d] shadow-[0_18px_48px_rgba(255,208,88,0.24)]"
            type="button"
            @click="openPlayer(activeMovie, activeEpisode)"
          >
            <Play :size="28" fill="currentColor" />
          </button>
          <div class="mt-6 flex flex-wrap gap-3">
            <button
              class="rounded-full bg-white px-5 py-2 text-sm font-black text-[#11131d] transition hover:bg-[#ffe182]"
              type="button"
              @click="openPlayer(activeMovie, activeEpisode)"
            >
              Xem phim
            </button>
            <button
              class="rounded-full border border-white/16 bg-white/8 px-5 py-2 text-sm font-bold text-white transition hover:border-[#ffe182] hover:text-[#ffe182]"
              type="button"
              @click="showHome"
            >
              Quay lại danh sách
            </button>
          </div>

          <div v-if="activeEpisodes.length" class="mt-8">
            <div class="mb-3 flex flex-wrap items-center gap-3">
              <h2 class="text-lg font-black text-white">Chọn tập</h2>
              <span class="rounded-full bg-white/9 px-3 py-1 text-xs font-bold text-slate-300">
                {{ activeEpisodes.length }} tập
              </span>
            </div>
            <div class="grid max-w-[560px] grid-cols-3 gap-2 sm:grid-cols-4 md:grid-cols-6">
              <button
                v-for="episode in activeEpisodes"
                :key="episode.id"
                :class="[
                  'h-10 rounded-lg border px-3 text-sm font-black transition',
                  activeEpisode?.id === episode.id
                    ? 'border-[#ffe182] bg-[#ffe182] text-[#11131d]'
                    : 'border-white/14 bg-white/9 text-white hover:border-[#ffe182] hover:text-[#ffe182]',
                ]"
                type="button"
                @click="selectEpisode(episode, true)"
              >
                Tập {{ episode.number }}
              </button>
            </div>
          </div>
        </div>

        <div class="absolute right-5 bottom-6 left-5 z-[1] flex justify-center gap-4 overflow-x-auto">
          <button
            v-for="movie in featuredMovies"
            :key="movie.id"
            :class="[
              'h-32 w-20 shrink-0 overflow-hidden rounded-lg border-2 bg-[#20232d] p-0',
              movie.id === activeMovie.id ? 'border-white' : 'border-white/10',
            ]"
            type="button"
            @click="openMovie(movie)"
          >
            <img class="h-full w-full object-cover" :src="movie.poster" :alt="movie.title" @error="useFallbackImage" />
          </button>
        </div>
      </section>

    </main>

    <footer class="relative overflow-hidden border-t border-white/6 bg-[#090a13]">
      <div
        class="pointer-events-none absolute left-1/2 top-0 hidden h-[360px] w-[360px] -translate-x-1/4 -translate-y-5 rounded-full border border-white/7 md:block"
      ></div>
      <img
        class="pointer-events-none absolute left-1/2 top-0 hidden h-[370px] w-[370px] -translate-x-1/4 object-contain opacity-[0.045] grayscale md:block"
        :src="logoUrl"
        alt=""
      />

      <div class="relative z-[1] mx-auto max-w-[1800px] px-5 py-12 md:px-[clamp(20px,7vw,140px)] md:py-16 2xl:px-[clamp(20px,14vw,370px)]">
        <div class="inline-flex max-w-full items-center gap-2 rounded-full bg-red-700 px-4 py-2 text-xs font-bold text-white shadow-[0_10px_30px_rgba(185,28,28,0.22)] md:text-sm">
          <Star class="shrink-0 text-[#ffe182]" :size="16" fill="currentColor" />
          <span class="truncate">ZMovie Cập Nhật Phim Hay Full HD Mỗi Ngày</span>
          <Heart class="shrink-0" :size="15" fill="currentColor" />
        </div>

        <img class="mt-8 h-[72px] w-[210px] object-contain object-left" :src="logoUrl" alt="ZMovie" />

        <nav class="mt-8 flex flex-wrap gap-x-8 gap-y-3 text-sm font-bold text-white">
          <button class="transition hover:text-[#ffe182]" type="button" @click="showHome">ZMovie</button>
          <button class="transition hover:text-[#ffe182]" type="button" @click="showHome">ZMovie Mới</button>
          <button class="transition hover:text-[#ffe182]" type="button" @click="selectCategory('Phim Lẻ Mới')">Phim Lẻ</button>
          <button class="transition hover:text-[#ffe182]" type="button" @click="selectCategory('Phim Bộ Mới')">Phim Bộ</button>
          <button
            class="transition hover:text-[#ffe182]"
            type="button"
            @click="selectTopic({ slug: 'tvshow', title: 'TV Show', query: 'TV Show', aliases: ['show', 'sân khấu'], hint: 'Show' })"
          >
            ZMovie TV
          </button>
        </nav>

        <p class="mt-6 max-w-3xl text-sm leading-7 text-slate-400">
          ZMovie - Nơi xem phim online chất lượng cao, tốc độ nhanh và hoàn toàn
          miễn phí. Tải đây, bạn có thể thưởng thức phim hay, phim HD, phim
          Vietsub và phim full với kho nội dung đồ sộ gồm phim chiếu rạp, phim
          bộ, phim lẻ đến từ nhiều quốc gia như Việt Nam, Hàn Quốc, Trung Quốc,
          Thái Lan, Nhật Bản, Âu Mỹ cùng hàng trăm thể loại hấp dẫn.
        </p>

        <p class="mt-3 max-w-3xl text-sm leading-7 text-slate-400">
          Trải nghiệm nền tảng phim trực tuyến hàng đầu năm 2024 với chất lượng
          hình ảnh lên đến 4K, âm thanh sống động và tốc độ tải cực nhanh.
        </p>

        <p class="mt-5 text-sm text-slate-500">© 2025 ZMovie</p>
      </div>
    </footer>

    <button
      class="fixed right-4 bottom-4 z-10 inline-flex h-12 w-12 cursor-pointer items-center justify-center rounded-full border border-white/16 bg-white text-[#0f111a] shadow-[0_16px_44px_rgba(0,0,0,0.28)] transition hover:bg-[#ffe182] md:right-6 md:bottom-6"
      type="button"
      aria-label="Lên đầu trang"
      @click="scrollToTop"
    >
      <ChevronDown class="rotate-180" :size="18" />
    </button>

  </div>
</template>
