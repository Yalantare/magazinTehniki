<template>
  <div class="app-container">
    <router-view />
    <ChatWidget v-if="showChat" />
    <ToastNotification />
  </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted } from 'vue'
import { useRoute } from 'vue-router'
import { store } from './store.js'
import ChatWidget from './components/ChatWidget.vue'
import ToastNotification from './components/ToastNotification.vue'

const route = useRoute()

// Показывать ИИ-чат только в каталоге товаров, просмотре товара и личном кабинете
const showChat = computed(() => {
  const path = route.path
  return path === '/catalog' || path.startsWith('/product/') || path === '/profile'
})

let lastRefreshAt = 0
function refreshFromDb() {
  const now = Date.now()
  if (now - lastRefreshAt < 1500) return
  lastRefreshAt = now
  store.loadFromApi(true)
}

function handleVisibilityChange() {
  if (document.visibilityState === 'visible') {
    refreshFromDb()
  }
}

onMounted(() => {
  store.initTheme()
  store.loadFromApi()
  window.addEventListener('focus', refreshFromDb)
  document.addEventListener('visibilitychange', handleVisibilityChange)
})

onUnmounted(() => {
  window.removeEventListener('focus', refreshFromDb)
  document.removeEventListener('visibilitychange', handleVisibilityChange)
})
</script>
