import { createRouter, createWebHistory } from 'vue-router'
import Auth from './pages/Auth.vue'
import Catalog from './pages/Catalog.vue'
import Product from './pages/Product.vue'
import Cart from './pages/Cart.vue'
import Checkout from './pages/Checkout.vue'
import Receipt from './pages/Receipt.vue'
import Profile from './pages/Profile.vue'

const routes = [
  { path: '/', component: Auth },
  { path: '/catalog', component: Catalog },
  { path: '/product/:id', component: Product },
  { path: '/cart', component: Cart },
  { path: '/checkout', component: Checkout },
  { path: '/receipt/:id', component: Receipt },
  { path: '/profile', component: Profile },
  { path: '/:pathMatch(.*)*', redirect: '/catalog' }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

export default router
