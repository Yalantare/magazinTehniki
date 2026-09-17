<template>
  <div class="item-card">
    <div class="image-wrapper">
      <img
        :src="item.product.photo || '/images/default.png'"
        :alt="item.product.title"
        class="item-image"
        @error="onImgError"
      />
    </div>

    <div class="details">
      <span class="manufacturer">{{ item.product.manufacturer }}</span>
      <h3 class="title">{{ item.product.title }}</h3>

      <span v-if="item.productVariation" class="variation">
        Вариация: {{ item.productVariation.name }}
      </span>

      <span class="stock-info">
        {{ item.product.categoryNavigation?.title || 'Категория' }}, {{ item.displayStock }} в наличии
      </span>

      <div class="qty-control">
        <button class="qty-btn" @click="handleDecrease" title="Уменьшить">
          -
        </button>
        <div class="qty-divider" />
        <span class="qty-value">{{ item.quantity }}</span>
        <div class="qty-divider" />
        <button
          class="qty-btn"
          :disabled="item.quantity >= item.displayStock"
          @click="handleIncrease"
          title="Увеличить"
        >
          +
        </button>
      </div>
    </div>

    <div class="right-section">
      <span class="price">
        {{ formatPrice(item.displayPrice * item.quantity) }}
      </span>
      <button
        class="remove-btn"
        @click="store.removeFromCart(item.id)"
        title="Удалить из корзины"
      >
        <Trash2 :size="18" />
      </button>
    </div>
  </div>
</template>

<script setup>
import { Trash2 } from 'lucide-vue-next'
import { store } from '../store.js'

const props = defineProps({
  item: {
    type: Object,
    required: true
  }
})

const formatPrice = (price) => {
  return price.toLocaleString('ru-RU') + ' ₽'
}

const onImgError = (e) => {
  e.target.src = '/images/default.png'
}

const handleDecrease = () => {
  store.updateQuantity(props.item.id, props.item.quantity - 1)
}

const handleIncrease = () => {
  store.updateQuantity(props.item.id, props.item.quantity + 1)
}
</script>

<style scoped>
.item-card {
  background-color: var(--theme-card-bg);
  border: 1px solid var(--theme-border);
  border-radius: 12px;
  padding: 20px;
  margin-bottom: 15px;
  display: grid;
  grid-template-columns: 80px 1fr auto;
  gap: 20px;
  align-items: center;
  transition: border-color 0.15s ease;
}

.item-card:hover {
  border-color: var(--theme-border-light);
}

.image-wrapper {
  width: 80px;
  height: 80px;
  border-radius: 8px;
  overflow: hidden;
  background-color: var(--theme-panel-bg);
  display: flex;
  align-items: center;
  justify-content: center;
}

.item-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.details {
  display: flex;
  flex-direction: column;
}

.manufacturer {
  font-size: 14px;
  color: var(--theme-text-secondary);
}

.title {
  font-size: 18px;
  font-weight: 700;
  color: var(--theme-text-primary);
  margin: 2px 0 4px 0;
}

.variation {
  font-size: 14px;
  font-weight: 600;
  color: var(--theme-accent);
  margin-bottom: 4px;
}

.stock-info {
  font-size: 14px;
  color: var(--theme-text-secondary);
  margin-bottom: 12px;
}

.qty-control {
  display: inline-flex;
  align-items: center;
  border: 1px solid var(--theme-border);
  border-radius: 6px;
  background-color: var(--theme-textbox-bg);
  height: 30px;
  width: fit-content;
}

.qty-btn {
  width: 30px;
  height: 100%;
  background: transparent;
  color: var(--theme-text-secondary);
  font-size: 16px;
  font-weight: 600;
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

.qty-value {
  width: 34px;
  text-align: center;
  font-size: 14px;
  font-weight: 600;
  color: var(--theme-text-primary);
}

.right-section {
  display: flex;
  align-items: center;
  gap: 25px;
}

.price {
  font-size: 20px;
  font-weight: 800;
  color: var(--theme-text-primary);
  white-space: nowrap;
}

.remove-btn {
  background: transparent;
  color: var(--theme-text-muted);
  width: 36px;
  height: 36px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: color 0.15s, background-color 0.15s;
}

.remove-btn:hover {
  color: var(--theme-danger);
  background-color: var(--theme-danger-bg);
}

@media (max-width: 600px) {
  .item-card {
    grid-template-columns: 1fr;
    gap: 12px;
  }
  .right-section {
    justify-content: space-between;
    margin-top: 10px;
  }
}
</style>
