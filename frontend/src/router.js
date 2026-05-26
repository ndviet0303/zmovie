import { createRouter, createWebHistory } from 'vue-router'

const Shell = { template: '<div />' }

export const routes = [
  { path: '/', name: 'home', component: Shell },
  { path: '/phim/:id', name: 'movie-detail', component: Shell },
  { path: '/xem/:id', name: 'watch', component: Shell },
  { path: '/danh-muc/:slug', name: 'category', component: Shell },
  { path: '/chu-de/:slug', name: 'topic', component: Shell },
  { path: '/the-loai/:slug', redirect: (to) => ({ name: 'topic', params: to.params }) },
  { path: '/tim-kiem', name: 'search', component: Shell },
  { path: '/admin/login', name: 'admin-login', component: Shell },
  { path: '/admin/movies/create', name: 'admin-movies-create', component: Shell },
  { path: '/admin/uploads', name: 'admin-uploads', component: Shell },
  { path: '/admin/licenses', name: 'admin-licenses', component: Shell },
  { path: '/admin/providers', name: 'admin-providers', component: Shell },
  { path: '/admin/legal', name: 'admin-legal', component: Shell },
  { path: '/admin/rbac', name: 'admin-rbac', component: Shell },
  { path: '/admin', name: 'admin', component: Shell },
  { path: '/:pathMatch(.*)*', redirect: '/' },
]

export const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior() {
    return { top: 0 }
  },
})
