<template>
  <div class="app-container">
    <router-view />
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import { store } from './store.js'

onMounted(() => {
  store.initTheme()
  store.loadFromApi()
})
</script>
