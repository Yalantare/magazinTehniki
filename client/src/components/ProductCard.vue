<template>
  <div class="card" @click="handleCardClick">
    <div class="availability-badge">
      <span
        class="availability-indicator"
        :style="{ backgroundColor: availabilityColor }"
      />
      <span :style="{ color: availabilityColor }">{{ availabilityText }}</span>
    </div>

    <div class="image-wrapper">
      <img
        :src="product.photo || '/images/default.png'"
        :alt="product.title"
        class="product-image"
        @error="onImgError"
      />
    </div>

    <div class="info-section">
      <span class="manufacturer">{{ product.manufacturer }}</span>
      <h3 class="title">{{ product.title }}</h3>

      <div class="rating-row">
        <span class="star-icon">
          <Star :size="14" fill="#F59E0B" color="#F59E0B" />
        </span>
        <span class="rating-value">{{ ratingInfo.rating.toFixed(1) }}</span>
        <span class="reviews-count">
          ({{ ratingInfo.count }})
        </span>
      </div>
    </div>

    <div class="bottom-section">
      <span class="price">{{ formatPrice(product.price) }}</span>
      <button
        class="add-btn"
        :disabled="!isAvailable"
        @click="handleAddClick"
      >
        {{ isAvailable ? 'Добавить' : 'Нет на складе' }}
      </button>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { Star } from 'lucide-vue-next'
import { store } from '../store.js'

const props = defineProps({
  product: {
    type: Object,
    required: true
  }
})

const router = useRouter()

const ratingInfo = computed(() => {
  return store.getRating(props.product.articul)
})

const isAvailable = computed(() => {
  return props.product.stock > 0
})

const availabilityColor = computed(() => {
  return isAvailable.value ? '#10B981' : '#EF4444'
})

const availabilityText = computed(() => {
  return isAvailable.value ? 'В наличии' : 'Нет в наличии'
})

const formatPrice = (price) => {
  return price.toLocaleString('ru-RU') + ' ₽'
}

const onImgError = (e) => {
  e.target.src = '/images/default.png'
}

const handleCardClick = () => {
  router.push(`/product/${props.product.articul}`)
}

const handleAddClick = (e) => {
  e.stopPropagation()
  if (!isAvailable.value) return

  if (props.product.productVariations && props.product.productVariations.length > 0) {
    router.push(`/product/${props.product.articul}`)
    return
  }

  store.addToCart(props.product)
}
</script>

<style scoped>
.card {
  background-color: var(--theme-card-bg);
  border: 1px solid var(--theme-border);
  border-radius: 12px;
  padding: 16px;
  display: flex;
  flex-direction: column;
  position: relative;
  cursor: pointer;
  transition: transform 0.2s ease, border-color 0.2s ease, box-shadow 0.2s ease;
}

.card:hover {
  transform: translateY(-4px);
  border-color: var(--theme-border-light);
  box-shadow: var(--theme-card-shadow);
}

.availability-badge {
  position: absolute;
  top: 26px;
  left: 26px;
  z-index: 10;
  background-color: rgba(15, 23, 42, 0.85);
  padding: 4px 8px;
  border-radius: 4px;
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  font-weight: 500;
  backdrop-filter: blur(4px);
}

.availability-indicator {
  width: 7px;
  height: 7px;
  border-radius: 2px;
}

.image-wrapper {
  width: 100%;
  height: 240px;
  border-radius: 12px;
  overflow: hidden;
  background-color: var(--theme-panel-bg);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 12px;
}

.product-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.3s ease;
}

.card:hover .product-image {
  transform: scale(1.03);
}

.info-section {
  flex: 1;
  display: flex;
  flex-direction: column;
  margin-bottom: 16px;
}

.manufacturer {
  font-size: 13px;
  color: var(--theme-text-secondary);
  font-weight: 500;
  margin-bottom: 4px;
}

.title {
  font-size: 17px;
  font-weight: 700;
  color: var(--theme-text-primary);
  line-height: 1.3;
  margin-bottom: 8px;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.rating-row {
  display: flex;
  align-items: center;
  gap: 4px;
}

.star-icon {
  display: flex;
  align-items: center;
}

.rating-value {
  font-size: 13px;
  font-weight: 700;
  color: var(--theme-text-primary);
}

.reviews-count {
  font-size: 13px;
  color: var(--theme-text-muted);
}

.bottom-section {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: auto;
  padding-top: 12px;
  border-top: 1px solid var(--theme-border);
}

.price {
  font-size: 18px;
  font-weight: 800;
  color: var(--theme-text-primary);
}

.add-btn {
  background-color: var(--theme-accent);
  color: #ffffff;
  padding: 8px 16px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.15s ease, opacity 0.15s ease;
}

.add-btn:hover:not(:disabled) {
  background-color: var(--theme-accent-hover);
}

.add-btn:disabled {
  background-color: var(--theme-textbox-bg);
  color: var(--theme-text-muted);
  cursor: not-allowed;
  border: 1px solid var(--theme-border);
}
</style>
