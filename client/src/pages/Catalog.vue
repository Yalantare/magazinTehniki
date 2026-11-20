<template>
  <div class="container">
    <Header />

    <div class="content-grid">
      <aside class="filters-sidebar">
        <h2 class="filters-title">Фильтры</h2>

        <div class="filter-section">
          <label class="filter-item availability-filter">
            <input
              type="checkbox"
              v-model="onlyInStock"
              class="checkbox"
            />
            <span class="filter-label">Только в наличии</span>
          </label>
        </div>

        <h3 class="filter-group-title" style="margin-top: 20px;">Категории</h3>
        <div class="filter-list">
          <label
            v-for="category in availableCategories"
            :key="category.id"
            class="filter-item"
          >
            <input
              type="checkbox"
              :value="category.id"
              v-model="selectedCategories"
              class="checkbox"
            />
            <span class="filter-label">{{ category.title }}</span>
            <span class="filter-count">{{ getCategoryCount(category.id) }}</span>
          </label>
          <div v-if="availableCategories.length === 0 && !store.isLoading" class="empty-filter-hint">
            Нет категорий
          </div>
        </div>

        <h3 class="filter-group-title" style="margin-top: 24px;">
          Бренды
        </h3>
        <div class="filter-list">
          <label
            v-for="brand in availableBrands"
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
            <span class="filter-count">{{ getBrandCount(brand) }}</span>
          </label>
          <div v-if="availableBrands.length === 0 && !store.isLoading" class="empty-filter-hint">
            Нет брендов
          </div>
        </div>

        <h3 class="filter-group-title" style="margin-top: 24px;">
          Цена, ₽
        </h3>
        <div class="price-filter-row">
          <input
            v-model="minPriceInput"
            type="number"
            min="0"
            :placeholder="priceBounds.min ? `от ${priceBounds.min}` : 'От'"
            class="price-input"
          />
          <span class="price-dash">—</span>
          <input
            v-model="maxPriceInput"
            type="number"
            min="0"
            :placeholder="priceBounds.max ? `до ${priceBounds.max}` : 'До'"
            class="price-input"
          />
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
            <option value="stock-desc">Сначала в наличии</option>
            <option value="rating-desc">По рейтингу (высокий сначала)</option>
            <option value="rating-asc">По рейтингу (низкий сначала)</option>
            <option value="price-asc">Цена: по возрастанию</option>
            <option value="price-desc">Цена: по убыванию</option>
            <option value="title-asc">Название: А-Я</option>
            <option value="title-desc">Название: Я-А</option>
          </select>
        </div>

        <div v-if="store.apiError && store.products.length === 0" class="no-results">
          <p>{{ store.apiError }}</p>
          <button class="retry-btn" @click="store.loadFromApi()">
            Повторить загрузку
          </button>
        </div>

        <div v-else-if="store.isLoading && store.products.length === 0" class="no-results">
          Загрузка каталога из базы данных...
        </div>

        <div v-else class="products-grid">
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
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { Search } from 'lucide-vue-next'
import Header from '../components/Header.vue'
import ProductCard from '../components/ProductCard.vue'
import { store } from '../store.js'
import { handleSpaceKeydown, sanitizeTextWithSpaces } from '../utils/validators.js'

onMounted(async () => {
  await store.loadFromApi(store.products.length > 0)
})

const selectedCategories = ref([])
const selectedBrands = ref([])
const onlyInStock = ref(false)
const minPriceInput = ref('')
const maxPriceInput = ref('')
const searchQuery = ref('')
const sortOption = ref('default')

const availableCategories = computed(() => {
  const map = new Map()
  for (const c of store.categories || []) {
    if (c && c.id != null) {
      map.set(Number(c.id), { id: Number(c.id), title: c.title })
    }
  }
  for (const p of store.products || []) {
    const cid = Number(p.categoryId ?? p.category ?? p.categoryNavigation?.id)
    const ctitle = p.categoryNavigation?.title
    if (cid && ctitle && !map.has(cid)) {
      map.set(cid, { id: cid, title: ctitle })
    }
  }
  return Array.from(map.values()).sort((a, b) => a.title.localeCompare(b.title, 'ru'))
})

const availableBrands = computed(() => {
  const set = new Set()
  for (const b of store.brands || []) {
    if (b && String(b).trim()) set.add(String(b).trim())
  }
  for (const p of store.products || []) {
    if (p.manufacturer && String(p.manufacturer).trim()) {
      set.add(String(p.manufacturer).trim())
    }
  }
  return Array.from(set).sort((a, b) => a.localeCompare(b, 'ru'))
})

const priceBounds = computed(() => {
  const prices = (store.products || [])
    .map(p => store.getProductDisplayPrice(p))
    .filter(v => Number.isFinite(v) && v > 0)
  if (prices.length === 0) return { min: 0, max: 0 }
  return {
    min: Math.floor(Math.min(...prices)),
    max: Math.ceil(Math.max(...prices))
  }
})

// Если в БД удалили категорию или бренд, автоматически убираем устаревший выбор
watch(availableCategories, (cats) => {
  const validIds = new Set(cats.map(c => c.id))
  selectedCategories.value = selectedCategories.value.filter(id => validIds.has(id))
})

watch(availableBrands, (brs) => {
  const valid = new Set(brs.map(b => b.toLowerCase()))
  selectedBrands.value = selectedBrands.value.filter(b => valid.has(String(b).toLowerCase()))
})

function getCategoryCount(categoryId) {
  return (store.products || []).filter(
    item => Number(item.categoryId ?? item.category ?? item.categoryNavigation?.id) === Number(categoryId)
  ).length
}

function getBrandCount(brand) {
  const target = String(brand).trim().toLowerCase()
  return (store.products || []).filter(
    item => String(item.manufacturer || '').trim().toLowerCase() === target
  ).length
}

function clearFilters() {
  selectedCategories.value = []
  selectedBrands.value = []
  onlyInStock.value = false
  minPriceInput.value = ''
  maxPriceInput.value = ''
  searchQuery.value = ''
  sortOption.value = 'default'
}

const hasFilters = computed(() => {
  return (
    selectedCategories.value.length > 0 ||
    selectedBrands.value.length > 0 ||
    onlyInStock.value ||
    minPriceInput.value !== '' ||
    maxPriceInput.value !== '' ||
    searchQuery.value !== '' ||
    sortOption.value !== 'default'
  )
})

const filteredProducts = computed(() => {
  let list = [...(store.products || [])]

  if (onlyInStock.value) {
    list = list.filter(item => store.getProductStock(item) > 0)
  }

  if (searchQuery.value) {
    const text = searchQuery.value.toLowerCase().trim()
    list = list.filter(item => {
      return (
        String(item.title || '').toLowerCase().includes(text) ||
        String(item.manufacturer || '').toLowerCase().includes(text) ||
        String(item.description || '').toLowerCase().includes(text) ||
        String(item.categoryNavigation?.title || '').toLowerCase().includes(text)
      )
    })
  }

  if (selectedCategories.value.length > 0) {
    const catSet = new Set(selectedCategories.value.map(Number))
    list = list.filter(item => {
      const cid = Number(item.categoryId ?? item.category ?? item.categoryNavigation?.id)
      return catSet.has(cid)
    })
  }

  if (selectedBrands.value.length > 0) {
    const brandSet = new Set(selectedBrands.value.map(b => String(b).trim().toLowerCase()))
    list = list.filter(item => {
      return brandSet.has(String(item.manufacturer || '').trim().toLowerCase())
    })
  }

  const minP = minPriceInput.value !== '' ? Number(minPriceInput.value) : null
  const maxP = maxPriceInput.value !== '' ? Number(maxPriceInput.value) : null

  if (minP !== null && Number.isFinite(minP) && minP >= 0) {
    list = list.filter(item => store.getProductDisplayPrice(item) >= minP)
  }
  if (maxP !== null && Number.isFinite(maxP) && maxP >= 0) {
    list = list.filter(item => store.getProductDisplayPrice(item) <= maxP)
  }

  if (sortOption.value === 'stock-desc') {
    list.sort((a, b) => store.getProductStock(b) - store.getProductStock(a))
  } else if (sortOption.value === 'rating-desc') {
    list.sort((a, b) => store.getRating(b.articul).rating - store.getRating(a.articul).rating)
  } else if (sortOption.value === 'rating-asc') {
    list.sort((a, b) => store.getRating(a.articul).rating - store.getRating(b.articul).rating)
  } else if (sortOption.value === 'price-asc') {
    list.sort((a, b) => store.getProductDisplayPrice(a) - store.getProductDisplayPrice(b))
  } else if (sortOption.value === 'price-desc') {
    list.sort((a, b) => store.getProductDisplayPrice(b) - store.getProductDisplayPrice(a))
  } else if (sortOption.value === 'title-asc') {
    list.sort((a, b) => String(a.title || '').localeCompare(String(b.title || ''), 'ru'))
  } else if (sortOption.value === 'title-desc') {
    list.sort((a, b) => String(b.title || '').localeCompare(String(a.title || ''), 'ru'))
  } else {
    list.sort((a, b) => Number(a.articul) - Number(b.articul))
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
  margin-bottom: 18px;
}

.filter-section {
  padding-bottom: 14px;
  border-bottom: 1px solid var(--theme-border);
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
  flex: 1;
}

.filter-count {
  font-size: 12px;
  color: var(--theme-text-muted);
}

.empty-filter-hint {
  font-size: 13px;
  color: var(--theme-text-muted);
}

.price-filter-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.price-input {
  width: 100%;
  min-width: 0;
  background-color: var(--theme-textbox-bg);
  border: 1px solid var(--theme-border);
  color: var(--theme-text-primary);
  border-radius: 8px;
  padding: 8px 10px;
  font-size: 13px;
}

.price-input:focus {
  border-color: var(--theme-accent);
  outline: none;
}

.price-dash {
  color: var(--theme-text-muted);
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
  border: 1px solid var(--theme-border-light);
  color: var(--theme-text-primary);
  border-radius: 8px;
  padding: 12px 44px 12px 16px;
  font-size: 14px;
  min-width: 220px;
  max-width: 100%;
  transition: border-color 0.15s ease;
}

.sort-select:hover {
  border-color: var(--theme-text-muted);
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

.retry-btn {
  margin-top: 16px;
  background-color: var(--theme-accent);
  color: #ffffff;
  border: none;
  border-radius: 8px;
  padding: 10px 20px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
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
