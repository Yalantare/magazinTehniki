<template>
  <div class="container">
    <Header />

    <div class="cart-wrapper">
      <div class="cart-header">
        <h1 class="title">Корзина</h1>
        <p class="items-count">{{ store.getCartCount() }} товаров</p>
      </div>

      <div v-if="store.cart.length > 0" class="cart-grid">
        <div class="items-list">
          <CartItem
            v-for="item in store.cart"
            :key="item.id"
            :item="item"
          />
        </div>

        <div class="summary-card">
          <h2 class="summary-title">Сумма заказа</h2>

          <div class="summary-items-list">
            <div
              v-for="item in store.cart"
              :key="item.id"
              class="summary-item-row"
            >
              <span class="summary-item-text">
                {{ item.product.title }} x {{ item.quantity }}
              </span>
              <span class="summary-item-price">
                {{ (item.displayPrice * item.quantity).toLocaleString('ru-RU') }} ₽
              </span>
            </div>
          </div>

          <div class="divider" />

          <div class="calc-row">
            <span class="calc-label">Пред. сумма</span>
            <span class="calc-value">{{ store.getCartSubtotal().toLocaleString('ru-RU') }} ₽</span>
          </div>

          <div class="calc-row">
            <span class="calc-label">Доставка</span>
            <span class="free-delivery">
              {{ store.getDelivery() === 0 ? 'Бесплатно' : store.getDelivery() + ' ₽' }}
            </span>
          </div>

          <div class="divider" />

          <div class="total-row">
            <span class="total-label">Сумма</span>
            <span class="total-value">{{ store.getCartTotal().toLocaleString('ru-RU') }} ₽</span>
          </div>

          <button
            class="checkout-btn"
            @click="router.push('/checkout')"
          >
            Перейти к оформлению ->
          </button>
        </div>
      </div>

      <div v-else class="empty-state">
        <h2 class="empty-title">Ваша корзина пуста</h2>
        <p class="empty-subtitle">
          Добавьте товары из каталога, чтобы оформить заказ
        </p>
        <button
          class="catalog-btn"
          @click="router.push('/catalog')"
        >
          В каталог
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { useRouter } from 'vue-router'
import Header from '../components/Header.vue'
import CartItem from '../components/CartItem.vue'
import { store } from '../store.js'

const router = useRouter()
</script>

<style scoped>
.container {
  min-height: 100vh;
  background-color: var(--theme-bg);
  display: flex;
  flex-direction: column;
}

.cart-wrapper {
  max-width: 1200px;
  width: 100%;
  margin: 0 auto;
  padding: 40px 40px 80px 40px;
}

.cart-header {
  margin-bottom: 30px;
}

.title {
  font-size: 32px;
  font-weight: 800;
  color: var(--theme-text-primary);
  margin-bottom: 4px;
}

.items-count {
  font-size: 15px;
  color: var(--theme-text-secondary);
}

.cart-grid {
  display: grid;
  grid-template-columns: 1fr 380px;
  gap: 40px;
  align-items: flex-start;
}

.items-list {
  display: flex;
  flex-direction: column;
}

.summary-card {
  background-color: var(--theme-card-bg);
  border: 1px solid var(--theme-border);
  border-radius: 16px;
  padding: 30px;
  position: sticky;
  top: 100px;
}

.summary-title {
  font-size: 20px;
  font-weight: 700;
  color: var(--theme-text-primary);
  margin-bottom: 20px;
}

.summary-items-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 20px;
}

.summary-item-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 10px;
  font-size: 14px;
}

.summary-item-text {
  color: var(--theme-text-secondary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.summary-item-price {
  font-weight: 600;
  color: var(--theme-text-primary);
  white-space: nowrap;
}

.divider {
  height: 1px;
  background-color: var(--theme-border);
  margin: 16px 0;
}

.calc-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 14px;
  margin-bottom: 12px;
}

.calc-label {
  color: var(--theme-text-secondary);
}

.calc-value {
  color: var(--theme-text-primary);
  font-weight: 600;
}

.free-delivery {
  color: var(--theme-success);
  font-weight: 600;
}

.total-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 10px;
  margin-bottom: 24px;
}

.total-label {
  font-size: 18px;
  font-weight: 700;
  color: var(--theme-text-primary);
}

.total-value {
  font-size: 24px;
  font-weight: 800;
  color: var(--theme-text-primary);
}

.checkout-btn {
  width: 100%;
  background-color: var(--theme-accent);
  color: #ffffff;
  padding: 14px;
  border-radius: 8px;
  font-size: 16px;
  font-weight: 700;
  cursor: pointer;
  transition: background-color 0.15s ease;
}

.checkout-btn:hover {
  background-color: var(--theme-accent-hover);
}

.empty-state {
  text-align: center;
  padding: 80px 20px;
  background-color: var(--theme-card-bg);
  border: 1px solid var(--theme-border);
  border-radius: 16px;
}

.empty-title {
  font-size: 24px;
  font-weight: 700;
  color: var(--theme-text-primary);
  margin-bottom: 8px;
}

.empty-subtitle {
  font-size: 15px;
  color: var(--theme-text-secondary);
  margin-bottom: 24px;
}

.catalog-btn {
  background-color: var(--theme-accent);
  color: #ffffff;
  padding: 12px 28px;
  border-radius: 8px;
  font-size: 15px;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.15s ease;
}

.catalog-btn:hover {
  background-color: var(--theme-accent-hover);
}

@media (max-width: 900px) {
  .cart-wrapper {
    padding: 20px 16px;
  }
  .cart-grid {
    grid-template-columns: 1fr;
  }
  .summary-card {
    position: static;
  }
}
</style>
