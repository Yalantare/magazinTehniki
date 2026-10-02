<template>
  <div class="container">
    <Header />

    <div v-if="!product" class="not-found">
      <h2>Товар не найден</h2>
      <button class="back-btn-primary" @click="router.push('/catalog')">
        Вернуться в каталог
      </button>
    </div>

    <div v-else class="details-wrapper">
      <button class="back-btn" @click="router.push('/catalog')">
        <ArrowLeft :size="18" />
        Назад к каталогу
      </button>

      <div class="content-grid">
        <div class="image-card">
          <img
            :src="product.photo || '/images/default.png'"
            :alt="product.title"
            class="product-image"
            @error="e => e.target.src = '/images/default.png'"
          />
        </div>

        <div class="info-col">
          <span class="manufacturer">{{ product.manufacturer }}</span>
          <h1 class="title">{{ product.title }}</h1>

          <div class="price-row">
            <span class="price">{{ currentPrice.toLocaleString('ru-RU') }} ₽</span>
            <div class="availability-badge">
              <span
                class="availability-indicator"
                :style="{ backgroundColor: isAvailable ? '#10B981' : '#EF4444' }"
              />
              <span :style="{ color: isAvailable ? '#10B981' : '#EF4444' }">
                {{ isAvailable ? 'В наличии' : 'Нет в наличии' }}
              </span>
            </div>

            <div class="rating-badge">
              <span class="rating-badge-star">
                <Star :size="16" fill="#F59E0B" color="#F59E0B" />
              </span>
              <span>{{ ratingInfo.rating }}</span>
              <span class="rating-badge-count">
                ({{ ratingInfo.count }} отзывов)
              </span>
            </div>
          </div>

          <h2 class="section-title">Описание</h2>
          <p class="description">
            {{ product.description || 'Описание товара отсутствует.' }}
          </p>

          <div
            v-if="product.productVariations && product.productVariations.length > 0"
            class="variations-panel"
          >
            <h2 class="section-title">Варианты товара</h2>
            <div class="variations-list">
              <div
                v-for="variation in product.productVariations"
                :key="variation.id"
                :class="['variation-card', selectedVariation?.id === variation.id ? 'variation-card-active' : '']"
                @click="selectedVariation = variation; quantity = 1"
              >
                <div class="variation-name">{{ variation.name }}</div>
                <div class="variation-price">{{ variation.price.toLocaleString('ru-RU') }} ₽</div>
              </div>
            </div>
          </div>

          <div class="purchase-panel">
            <div class="quantity-box">
              <button
                class="qty-btn"
                :disabled="quantity <= 1 || !isAvailable"
                @click="quantity > 1 ? quantity-- : null"
              >
                -
              </button>
              <div class="qty-divider" />
              <span class="quantity-value">{{ quantity }}</span>
              <div class="qty-divider" />
              <button
                class="qty-btn"
                :disabled="quantity >= currentStock || !isAvailable"
                @click="quantity < currentStock ? quantity++ : null"
              >
                +
              </button>
            </div>

            <button
              class="add-to-cart-btn"
              :disabled="!isAvailable"
              @click="add"
            >
              {{ isAvailable ? 'Добавить в корзину' : 'Нет в наличии' }}
            </button>
          </div>

          <div v-if="successMsg" class="success-notice">
            <Check :size="18" />
            {{ successMsg }}
          </div>
        </div>
      </div>

      <section class="reviews-section">
        <div class="reviews-top-bar">
          <div class="reviews-title-block">
            <h2 class="reviews-title">Отзывы покупателей</h2>
            <span class="reviews-count-tag">
              ★ {{ ratingInfo.rating }} | {{ ratingInfo.count }} отзывов
            </span>
          </div>

          <button
            :class="['open-review-btn', isFormOpen ? 'open-review-btn-active' : '']"
            @click="isFormOpen = !isFormOpen"
          >
            <MessageSquarePlus :size="18" />
            {{ isFormOpen ? 'Скрыть форму' : 'Оставить отзыв' }}
            <ChevronUp v-if="isFormOpen" :size="16" />
            <ChevronDown v-else :size="16" />
          </button>
        </div>

        <div v-if="reviewMsg" class="success-notice" style="margin-bottom: 20px;">
          <Check :size="18" />
          {{ reviewMsg }}
        </div>

        <div v-if="isFormOpen" class="review-form-wrapper">
          <form class="review-form" @submit.prevent="sendReview">
            <h3 class="form-section-title">Ваш отзыв о товаре</h3>

            <div class="rating-select-row">
              <span class="rating-select-label">Оценка:</span>
              <div class="stars-interactive">
                <button
                  v-for="score in [1, 2, 3, 4, 5]"
                  :key="score"
                  type="button"
                  class="star-btn"
                  @click="ratingScore = score"
                >
                  <Star
                    :size="22"
                    :fill="score <= ratingScore ? '#F59E0B' : 'transparent'"
                    :color="score <= ratingScore ? '#F59E0B' : 'var(--theme-border-light)'"
                  />
                </button>
              </div>
            </div>

            <div v-if="!store.user" class="form-input-group">
              <label class="form-label">Ваше имя</label>
              <input
                v-model="authorName"
                type="text"
                maxlength="50"
                placeholder="Как к вам обращаться?"
                class="form-input"
                @keydown="handleSpaceKeydown($event, authorName, 2)"
                @input="authorName = sanitizeName($event.target.value, 2, 50)"
              />
            </div>

            <div class="form-input-group">
              <label class="form-label">Комментарий *</label>
              <textarea
                v-model="commentText"
                rows="4"
                maxlength="500"
                placeholder="Поделитесь вашими впечатлениями от использования..."
                class="form-textarea"
                required
                @keydown="handleSpaceKeydown($event, commentText, 100)"
                @input="commentText = commentText.replace(/\s{3,}/g, '  ').slice(0, 500)"
              />
            </div>

            <div class="form-actions">
              <button type="submit" class="submit-review-btn">
                Опубликовать отзыв
              </button>
            </div>
          </form>
        </div>

        <div class="reviews-list">
          <div
            v-for="review in reviewsList"
            :key="review.id"
            class="review-card"
          >
            <div class="review-card-header">
              <div class="review-author-info">
                <div class="avatar-box">
                  {{ review.userName.charAt(0).toUpperCase() }}
                </div>
                <div class="author-meta">
                  <div class="author-name-row">
                    <span class="author-name">{{ review.userName }}</span>
                    <span class="verified-badge">Проверенный покупатель</span>
                  </div>
                  <span class="review-date">{{ review.date }}</span>
                </div>
              </div>

              <div class="review-stars">
                <Star
                  v-for="starNum in [1, 2, 3, 4, 5]"
                  :key="starNum"
                  :size="16"
                  :fill="starNum <= review.rating ? '#F59E0B' : 'transparent'"
                  :color="starNum <= review.rating ? '#F59E0B' : 'var(--theme-border)'"
                />
              </div>
            </div>

            <p v-if="review.comment" class="review-comment-text">
              {{ review.comment }}
            </p>
          </div>

          <div v-if="reviewsList.length === 0" class="no-reviews-notice">
            Пока никто не оставил отзыв об этом товаре. Будьте первыми!
          </div>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ArrowLeft, Check, Star, MessageSquarePlus, ChevronDown, ChevronUp } from 'lucide-vue-next'
import Header from '../components/Header.vue'
import { store } from '../store.js'
import { handleSpaceKeydown, sanitizeName } from '../utils/validators.js'

const route = useRoute()
const router = useRouter()

const product = computed(() => {
  return store.products.find(p => String(p.articul) === String(route.params.id))
})

const selectedVariation = ref(
  product.value?.productVariations ? product.value.productVariations[0] : null
)

watch(product, (newVal) => {
  if (newVal?.productVariations?.length && !selectedVariation.value) {
    selectedVariation.value = newVal.productVariations[0]
  }
  if (newVal?.articul) {
    store.loadReviews(newVal.articul)
  }
}, { immediate: true })

watch(() => route.params.id, (newId) => {
  if (newId) {
    const p = store.products.find(x => String(x.articul) === String(newId))
    selectedVariation.value = p?.productVariations?.length ? p.productVariations[0] : null
    quantity.value = 1
    if (p?.articul) {
      store.loadReviews(p.articul)
    }
  }
})

onMounted(() => {
  if (product.value?.articul) {
    store.loadReviews(product.value.articul)
  }
})

const quantity = ref(1)
const successMsg = ref('')

const isFormOpen = ref(false)
const ratingScore = ref(5)
const authorName = ref('')
const commentText = ref('')
const reviewMsg = ref('')

const currentPrice = computed(() => {
  return selectedVariation.value ? selectedVariation.value.price : (product.value?.price || 0)
})

const currentStock = computed(() => {
  return selectedVariation.value ? selectedVariation.value.stock : (product.value?.stock || 0)
})

const isAvailable = computed(() => {
  return currentStock.value > 0
})

const ratingInfo = computed(() => {
  if (!product.value) return { rating: 5, count: 0 }
  return store.getRating(product.value.articul)
})

const reviewsList = computed(() => {
  if (!product.value) return []
  return store.getReviews(product.value.articul)
})

function add() {
  if (!product.value || !isAvailable.value) return
  store.addToCart(product.value, selectedVariation.value, quantity.value)
  successMsg.value = 'Добавлено в корзину: ' + quantity.value + ' шт.'
  setTimeout(() => {
    successMsg.value = ''
  }, 3000)
}

async function sendReview() {
  if (!product.value || !commentText.value) return
  const name = authorName.value.trim() || store.user?.name || 'Покупатель'
  await store.addReview(product.value.articul, ratingScore.value, commentText.value, name)
  commentText.value = ''
  authorName.value = ''
  isFormOpen.value = false
  reviewMsg.value = 'Спасибо за отзыв!'
  setTimeout(() => {
    reviewMsg.value = ''
  }, 3000)
}
</script>

<style scoped>
.container {
  min-height: 100vh;
  background-color: var(--theme-bg);
  display: flex;
  flex-direction: column;
}

.not-found {
  padding: 60px;
  text-align: center;
}

.back-btn-primary {
  margin-top: 20px;
  padding: 10px 20px;
  background-color: var(--theme-accent);
  color: #fff;
  border-radius: 8px;
  font-weight: 600;
}

.details-wrapper {
  max-width: 1200px;
  width: 100%;
  margin: 0 auto;
  padding: 30px 40px 60px 40px;
}

.back-btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: transparent;
  color: var(--theme-text-secondary);
  font-size: 15px;
  font-weight: 600;
  cursor: pointer;
  margin-bottom: 25px;
  padding: 6px 12px;
  border-radius: 8px;
  transition: all 0.15s ease;
}

.back-btn:hover {
  color: var(--theme-text-primary);
  background-color: var(--theme-panel-bg);
}

.content-grid {
  display: grid;
  grid-template-columns: 1fr 1.2fr;
  gap: 50px;
  background-color: var(--theme-card-bg);
  border: 1px solid var(--theme-border);
  border-radius: 16px;
  padding: 40px;
  margin-bottom: 40px;
}

.image-card {
  width: 100%;
  height: 440px;
  background-color: var(--theme-panel-bg);
  border-radius: 12px;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--theme-border);
}

.product-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.info-col {
  display: flex;
  flex-direction: column;
}

.manufacturer {
  font-size: 14px;
  font-weight: 600;
  color: var(--theme-accent);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 6px;
}

.title {
  font-size: 32px;
  font-weight: 800;
  color: var(--theme-text-primary);
  line-height: 1.25;
  margin-bottom: 20px;
}

.price-row {
  display: flex;
  align-items: center;
  gap: 20px;
  margin-bottom: 25px;
  flex-wrap: wrap;
}

.price {
  font-size: 32px;
  font-weight: 800;
  color: var(--theme-text-primary);
}

.availability-badge {
  background-color: var(--theme-panel-bg);
  border: 1px solid var(--theme-border);
  padding: 6px 12px;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  font-weight: 700;
}

.availability-indicator {
  width: 7px;
  height: 7px;
  border-radius: 2px;
}

.rating-badge {
  background-color: var(--theme-panel-bg);
  border: 1px solid var(--theme-border);
  padding: 6px 12px;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  font-weight: 700;
  color: var(--theme-text-primary);
}

.rating-badge-star {
  display: flex;
  align-items: center;
}

.rating-badge-count {
  font-size: 13px;
  color: var(--theme-text-muted);
  font-weight: 500;
}

.section-title {
  font-size: 20px;
  font-weight: 700;
  color: var(--theme-text-primary);
  margin-bottom: 10px;
}

.description {
  font-size: 15px;
  line-height: 1.6;
  color: var(--theme-text-secondary);
  margin-bottom: 25px;
}

.variations-panel {
  margin-bottom: 25px;
}

.variations-list {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  margin-top: 10px;
}

.variation-card {
  background-color: var(--theme-panel-bg);
  border: 1px solid var(--theme-border);
  border-radius: 8px;
  padding: 10px 16px;
  cursor: pointer;
  transition: all 0.15s ease;
}

.variation-card:hover {
  border-color: var(--theme-accent);
}

.variation-card-active {
  border-color: var(--theme-accent);
  background-color: var(--theme-textbox-bg);
  box-shadow: 0 0 0 1px var(--theme-accent);
}

.variation-name {
  font-size: 14px;
  font-weight: 600;
  color: var(--theme-text-primary);
}

.variation-price {
  font-size: 13px;
  color: var(--theme-text-secondary);
  margin-top: 2px;
}

.purchase-panel {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-top: auto;
  padding-top: 20px;
}

.quantity-box {
  display: inline-flex;
  align-items: center;
  border: 1px solid var(--theme-border);
  border-radius: 8px;
  background-color: var(--theme-textbox-bg);
  height: 48px;
}

.qty-btn {
  width: 44px;
  height: 100%;
  background: transparent;
  color: var(--theme-text-secondary);
  font-size: 18px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: color 0.15s;
}

.qty-btn:hover:not(:disabled) {
  color: var(--theme-text-primary);
}

.qty-btn:disabled {
  color: var(--theme-text-muted);
  cursor: not-allowed;
}

.qty-divider {
  width: 1px;
  height: 100%;
  background-color: var(--theme-border);
}

.quantity-value {
  width: 44px;
  text-align: center;
  font-size: 16px;
  font-weight: 700;
  color: var(--theme-text-primary);
}

.add-to-cart-btn {
  flex: 1;
  height: 48px;
  background-color: var(--theme-accent);
  color: #ffffff;
  border-radius: 8px;
  font-size: 16px;
  font-weight: 700;
  cursor: pointer;
  transition: background-color 0.15s ease;
}

.add-to-cart-btn:hover:not(:disabled) {
  background-color: var(--theme-accent-hover);
}

.add-to-cart-btn:disabled {
  background-color: var(--theme-textbox-bg);
  color: var(--theme-text-muted);
  cursor: not-allowed;
}

.success-notice {
  margin-top: 15px;
  background-color: var(--theme-success-bg);
  color: var(--theme-success);
  padding: 12px 16px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  font-weight: 600;
}

.reviews-section {
  background-color: var(--theme-card-bg);
  border: 1px solid var(--theme-border);
  border-radius: 16px;
  padding: 35px 40px;
}

.reviews-top-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 25px;
  flex-wrap: wrap;
  gap: 15px;
}

.reviews-title-block {
  display: flex;
  align-items: center;
  gap: 15px;
}

.reviews-title {
  font-size: 24px;
  font-weight: 700;
  color: var(--theme-text-primary);
}

.reviews-count-tag {
  background-color: var(--theme-panel-bg);
  border: 1px solid var(--theme-border);
  padding: 4px 10px;
  border-radius: 6px;
  font-size: 13px;
  font-weight: 700;
  color: var(--theme-text-secondary);
}

.open-review-btn {
  background-color: var(--theme-panel-bg);
  border: 1px solid var(--theme-border);
  color: var(--theme-text-primary);
  padding: 10px 18px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  transition: all 0.15s ease;
}

.open-review-btn:hover {
  background-color: var(--theme-textbox-bg);
  border-color: var(--theme-border-light);
}

.open-review-btn-active {
  border-color: var(--theme-accent);
}

.review-form-wrapper {
  margin-bottom: 30px;
}

.review-form {
  background-color: var(--theme-panel-bg);
  border: 1px solid var(--theme-border);
  border-radius: 12px;
  padding: 24px;
}

.form-section-title {
  font-size: 17px;
  font-weight: 700;
  color: var(--theme-text-primary);
  margin-bottom: 16px;
}

.rating-select-row {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}

.rating-select-label {
  font-size: 14px;
  font-weight: 600;
  color: var(--theme-text-secondary);
}

.stars-interactive {
  display: flex;
  gap: 4px;
}

.star-btn {
  background: transparent;
  padding: 2px;
  cursor: pointer;
  display: flex;
  align-items: center;
}

.form-input-group {
  margin-bottom: 16px;
}

.form-label {
  display: block;
  font-size: 13px;
  font-weight: 600;
  color: var(--theme-text-secondary);
  margin-bottom: 6px;
}

.form-input {
  width: 100%;
  background-color: var(--theme-textbox-bg);
  border: 1px solid var(--theme-border);
  color: var(--theme-text-primary);
  border-radius: 8px;
  padding: 10px 14px;
  font-size: 14px;
}

.form-textarea {
  width: 100%;
  background-color: var(--theme-textbox-bg);
  border: 1px solid var(--theme-border);
  color: var(--theme-text-primary);
  border-radius: 8px;
  padding: 12px 14px;
  font-size: 14px;
  resize: vertical;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
}

.submit-review-btn {
  background-color: var(--theme-accent);
  color: #ffffff;
  padding: 10px 24px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 700;
  cursor: pointer;
}

.reviews-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.review-card {
  background-color: var(--theme-panel-bg);
  border: 1px solid var(--theme-border);
  border-radius: 12px;
  padding: 20px;
}

.review-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.review-author-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.avatar-box {
  width: 38px;
  height: 38px;
  border-radius: 8px;
  background-color: var(--theme-accent);
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: 15px;
}

.author-meta {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.author-name-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.author-name {
  font-size: 15px;
  font-weight: 700;
  color: var(--theme-text-primary);
}

.verified-badge {
  font-size: 11px;
  background-color: var(--theme-success-bg);
  color: var(--theme-success);
  padding: 2px 6px;
  border-radius: 4px;
  font-weight: 600;
}

.review-date {
  font-size: 12px;
  color: var(--theme-text-muted);
}

.review-stars {
  display: flex;
  gap: 3px;
}

.review-comment-text {
  font-size: 14px;
  line-height: 1.6;
  color: var(--theme-text-primary);
}

.no-reviews-notice {
  padding: 40px;
  text-align: center;
  color: var(--theme-text-secondary);
  font-size: 15px;
}

@media (max-width: 900px) {
  .content-grid {
    grid-template-columns: 1fr;
    padding: 24px;
    gap: 30px;
  }
  .image-card {
    height: 300px;
  }
}
</style>
