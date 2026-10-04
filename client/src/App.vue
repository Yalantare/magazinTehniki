<template>
  <div class="app-container">
    <router-view />
    <ChatWidget v-if="showChat" />
  </div>
</template>

<script setup>
import { computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { store } from './store.js'
import ChatWidget from './components/ChatWidget.vue'

const route = useRoute()

// Показывать ИИ-чат только в каталоге товаров, просмотре товара и личном кабинете
const showChat = computed(() => {
  const path = route.path
  return path === '/catalog' || path.startsWith('/product/') || path === '/profile'
})

onMounted(() => {
  store.initTheme()
  store.loadFromApi()
})
</script>

