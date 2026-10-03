<template>
  <div class="container">
    <Header />

    <div class="content-grid">
      <aside class="filters-sidebar">
        <h2 class="filters-title">Фильтры</h2>

        <h3 class="filter-group-title">Категории</h3>
        <div class="filter-list">
          <label
            v-for="category in store.categories"
            :key="category.id"
            class="filter-item"
          >
            <input
              type="checkbox"
              :value="category.title"
              v-model="selectedCategories"
              class="checkbox"
            />
            <span class="filter-label">{{ category.title }}</span>
          </label>
        </div>

        <h3 class="filter-group-title" style="margin-top: 24px;">
          Бренды
        </h3>
        <div class="filter-list">
          <label
            v-for="brand in store.brands"
            :key="brand"
            class="filter-item"
          >
            <input
              type="checkbox"
              :value="brand"
              v-model="selectedBrands"
              class="checkbox"
            />
            <span class="filter-label">{{ brand }}</span>
          </label>
        </div>

        <button
          v-if="hasFilters"
          class="clear-filters-btn"
          @click="clearFilters"
        >
          Сбросить фильтры
        </button>
      </aside>

      <main class="catalog-main">
        <div class="top-controls">
          <div class="search-wrapper">
            <Search :size="18" class="search-icon" />
            <input
              v-model="searchQuery"
              type="text"
              maxlength="60"
              placeholder="Поиск по товарам..."
              class="search-input"
              @keydown="handleSpaceKeydown($event, searchQuery, 5)"
              @input="searchQuery = sanitizeTextWithSpaces($event.target.value, 5, 60)"
            />
          </div>

          <select v-model="sortOption" class="sort-select">
            <option value="default">Сортировка: По умолчанию</option>
            <option value="price-asc">Цена: по возрастанию</option>
            <option value="price-desc">Цена: по убыванию</option>
            <option value="title-asc">Название: А-Я</option>
            <option value="title-desc">Название: Я-А</option>
          </select>
        </div>

        <div class="products-grid">
          <ProductCard
            v-for="product in filteredProducts"
            :key="product.articul"
            :product="product"
          />
          <div v-if="filteredProducts.length === 0" class="no-results">
            По вашему запросу товары не найдены. Попробуйте изменить параметры фильтрации.
          </div>
        </div>
      </main>
    </div>

    <ChatBot />
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { Search } from 'lucide-vue-next'
import Header from '../components/Header.vue'
import ProductCard from '../components/ProductCard.vue'
import ChatBot from '../components/ChatBot.vue'
import { store } from '../store.js'
import { handleSpaceKeydown, sanitizeTextWithSpaces } from '../utils/validators.js'

const selectedCategories = ref([])
const selectedBrands = ref([])
const searchQuery = ref('')
const sortOption = ref('default')

function clearFilters() {
  selectedCategories.value = []
  selectedBrands.value = []
  searchQuery.value = ''
  sortOption.value = 'default'
}

const hasFilters = computed(() => {
  return (
    selectedCategories.value.length > 0 ||
    selectedBrands.value.length > 0 ||
    searchQuery.value !== '' ||
    sortOption.value !== 'default'
  )
})

const filteredProducts = computed(() => {
  let list = [...store.products]

  if (searchQuery.value) {
    const text = searchQuery.value.toLowerCase().trim()
    list = list.filter(item => {
      return (
        item.title.toLowerCase().includes(text) ||
        item.manufacturer.toLowerCase().includes(text) ||
        (item.description && item.description.toLowerCase().includes(text))
      )
    })
  }

  if (selectedCategories.value.length > 0) {
    list = list.filter(item => {
      return selectedCategories.value.includes(item.categoryNavigation?.title)
    })
  }

  if (selectedBrands.value.length > 0) {
    list = list.filter(item => {
      return selectedBrands.value.includes(item.manufacturer)
    })
  }

  if (sortOption.value === 'price-asc') {
    list.sort((a, b) => a.price - b.price)
  } else if (sortOption.value === 'price-desc') {
    list.sort((a, b) => b.price - a.price)
  } else if (sortOption.value === 'title-asc') {
    list.sort((a, b) => a.title.localeCompare(b.title))
  } else if (sortOption.value === 'title-desc') {
    list.sort((a, b) => b.title.localeCompare(a.title))
  } else {
    list.sort((a, b) => a.articul - b.articul)
  }

  return list
})
</script>

<style scoped>
.container {
  min-height: 100vh;
  background-color: var(--theme-bg);
  display: flex;
  flex-direction: column;
}

.content-grid {
  flex: 1;
  display: grid;
  grid-template-columns: 260px 1fr;
  max-width: 1440px;
  width: 100%;
  margin: 0 auto;
  padding: 30px 40px;
  gap: 40px;
}

.filters-sidebar {
  background-color: var(--theme-card-bg);
  border: 1px solid var(--theme-border);
  border-radius: 12px;
  padding: 24px;
  height: fit-content;
  position: sticky;
  top: 100px;
}

.filters-title {
  font-size: 20px;
  font-weight: 700;
  color: var(--theme-text-primary);
  margin-bottom: 20px;
}

.filter-group-title {
  font-size: 15px;
  font-weight: 600;
  color: var(--theme-text-secondary);
  margin-bottom: 12px;
}

.filter-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.filter-item {
  display: flex;
  align-items: center;
  gap: 10px;
  cursor: pointer;
  user-select: none;
}

.checkbox {
  width: 16px;
  height: 16px;
  accent-color: var(--theme-accent);
  cursor: pointer;
}

.filter-label {
  font-size: 14px;
  color: var(--theme-text-primary);
}

.clear-filters-btn {
  margin-top: 24px;
  width: 100%;
  background-color: transparent;
  border: 1px solid var(--theme-border-light);
  color: var(--theme-text-secondary);
  padding: 8px 14px;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s ease;
}

.clear-filters-btn:hover {
  background-color: var(--theme-panel-bg);
  color: var(--theme-text-primary);
}

.catalog-main {
  display: flex;
  flex-direction: column;
}

.top-controls {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  margin-bottom: 25px;
}

.search-wrapper {
  flex: 1;
  position: relative;
  display: flex;
  align-items: center;
}

.search-icon {
  position: absolute;
  left: 14px;
  color: var(--theme-text-muted);
  pointer-events: none;
}

.search-input {
  width: 100%;
  background-color: var(--theme-textbox-bg);
  border: 1px solid var(--theme-border);
  color: var(--theme-text-primary);
  border-radius: 8px;
  padding: 12px 14px 12px 42px;
  font-size: 14px;
  transition: border-color 0.15s ease;
}

.search-input:focus {
  border-color: var(--theme-accent);
}

.sort-select {
  background-color: var(--theme-combobox-bg);
  border: 1px solid var(--theme-border);
  color: var(--theme-text-primary);
  border-radius: 8px;
  padding: 12px 16px;
  font-size: 14px;
  cursor: pointer;
  outline: none;
  min-width: 220px;
}

.products-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 20px;
}

.no-results {
  grid-column: 1 / -1;
  padding: 60px 20px;
  text-align: center;
  color: var(--theme-text-secondary);
  font-size: 16px;
  background-color: var(--theme-card-bg);
  border-radius: 12px;
  border: 1px solid var(--theme-border);
}

@media (max-width: 900px) {
  .content-grid {
    grid-template-columns: 1fr;
    padding: 20px 16px;
  }
  .filters-sidebar {
    position: static;
  }
  .top-controls {
    flex-direction: column;
    align-items: stretch;
  }
}
</style>
