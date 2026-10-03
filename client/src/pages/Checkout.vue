<template>
  <div class="container">
    <Header />

    <div class="checkout-wrapper">
      <h1 class="title">Оформление заказа</h1>

      <div v-if="store.cart.length === 0" class="empty-state">
        <h2>Корзина пуста</h2>
        <p style="margin-top: 10px; color: var(--theme-text-secondary);">
          Добавьте товары, чтобы перейти к оформлению.
        </p>
        <button class="catalog-btn" @click="router.push('/catalog')">
          В каталог
        </button>
      </div>

      <form v-else class="checkout-grid" novalidate @submit.prevent="submitOrder">
        <div class="form-col">
          <div v-if="error" class="error-notice">
            {{ error }}
          </div>

          <div class="section-card">
            <h2 class="card-title">1. Контактные данные</h2>
            <div class="two-col-grid">
              <div class="input-group">
                <label class="label">ФИО получателя *</label>
                <input
                  v-model="name"
                  type="text"
                  maxlength="60"
                  placeholder="Иван Иванов"
                  class="input"
                  required
                  @keydown="handleSpaceKeydown($event, name, 2)"
                  @input="name = sanitizeName($event.target.value, 2, 60)"
                />
              </div>

              <div class="input-group">
                <label class="label">Телефон для связи *</label>
                <input
                  v-model="phone"
                  type="tel"
                  maxlength="18"
                  placeholder="+7 (999) 000-00-00"
                  class="input"
                  required
                  @keydown="handleSpaceKeydown($event, phone, 0)"
                  @input="phone = formatPhone($event.target.value)"
                />
              </div>
            </div>
          </div>

          <div class="section-card">
            <h2 class="card-title">2. Адрес доставки</h2>
            <div class="two-col-grid">
              <div class="input-group">
                <label class="label">Город *</label>
                <input
                  v-model="city"
                  type="text"
                  maxlength="50"
                  placeholder="г. Уфа"
                  class="input"
                  required
                  @keydown="handleSpaceKeydown($event, city, 2)"
                  @input="city = sanitizeTextWithSpaces($event.target.value, 2, 50)"
                />
              </div>

              <div class="input-group">
                <label class="label">Улица *</label>
                <input
                  v-model="street"
                  type="text"
                  maxlength="80"
                  placeholder="ул. Кирова"
                  class="input"
                  required
                  @keydown="handleSpaceKeydown($event, street, 4)"
                  @input="street = sanitizeTextWithSpaces($event.target.value, 4, 80)"
                />
              </div>
            </div>

            <div class="four-col-grid">
              <div class="input-group">
                <label class="label">Дом *</label>
                <input
                  v-model="house"
                  type="text"
                  maxlength="12"
                  placeholder="д. 65/2"
                  class="input"
                  required
                  @keydown="handleSpaceKeydown($event, house, 1)"
                  @input="house = sanitizeTextWithSpaces($event.target.value, 1, 12)"
                />
              </div>

              <div class="input-group">
                <label class="label">Этаж</label>
                <input
                  v-model="floor"
                  type="text"
                  maxlength="3"
                  placeholder="2"
                  class="input"
                  @keydown="handleSpaceKeydown($event, floor, 0)"
                  @input="floor = sanitizeDigits($event.target.value, 3)"
                />
              </div>

              <div class="input-group">
                <label class="label">Подъезд</label>
                <input
                  v-model="entrance"
                  type="text"
                  maxlength="3"
                  placeholder="1"
                  class="input"
                  @keydown="handleSpaceKeydown($event, entrance, 0)"
                  @input="entrance = sanitizeDigits($event.target.value, 3)"
                />
              </div>

              <div class="input-group">
                <label class="label">Кв. / Офис</label>
                <input
                  v-model="apartment"
                  type="text"
                  maxlength="10"
                  placeholder="кв. 1"
                  class="input"
                  @keydown="handleSpaceKeydown($event, apartment, 1)"
                  @input="apartment = sanitizeTextWithSpaces($event.target.value, 1, 10)"
                />
              </div>
            </div>
          </div>

          <div class="section-card">
            <h2 class="card-title">3. Способ оплаты</h2>
            <div class="payment-methods">
              <label
                :class="['payment-option', paymentMethod === 'card' ? 'payment-option-active' : '']"
              >
                <input
                  type="radio"
                  name="payment"
                  value="card"
                  v-model="paymentMethod"
                  class="radio"
                />
                <span class="payment-title">Банковская карта онлайн</span>
              </label>

              <label
                :class="['payment-option', paymentMethod === 'sbp' ? 'payment-option-active' : '']"
              >
                <input
                  type="radio"
                  name="payment"
                  value="sbp"
                  v-model="paymentMethod"
                  class="radio"
                />
                <span class="payment-title">СБП (Система быстрых платежей)</span>
              </label>

              <label
                :class="['payment-option', paymentMethod === 'cash' ? 'payment-option-active' : '']"
              >
                <input
                  type="radio"
                  name="payment"
                  value="cash"
                  v-model="paymentMethod"
                  class="radio"
                />
                <span class="payment-title">При получении заказа</span>
              </label>
            </div>

            <div v-if="paymentMethod === 'card'" class="card-inputs">
              <div class="input-group">
                <label class="label">Номер карты</label>
                <input
                  v-model="cardNumber"
                  type="text"
                  maxlength="19"
                  placeholder="0000 0000 0000 0000"
                  class="input"
                  @keydown="handleSpaceKeydown($event, cardNumber, 0)"
                  @input="cardNumber = sanitizeCardNumber($event.target.value)"
                />
              </div>

              <div class="two-col-grid">
                <div class="input-group">
                  <label class="label">Срок действия</label>
                  <input
                    v-model="cardExpiry"
                    type="text"
                    maxlength="5"
                    placeholder="ММ/ГГ"
                    class="input"
                    @keydown="handleSpaceKeydown($event, cardExpiry, 0)"
                    @input="cardExpiry = sanitizeCardExpiry($event.target.value)"
                  />
                </div>

                <div class="input-group">
                  <label class="label">CVV / CVC</label>
                  <input
                    v-model="cardCvv"
                    type="password"
                    maxlength="3"
                    placeholder="***"
                    class="input"
                    @keydown="handleSpaceKeydown($event, cardCvv, 0)"
                    @input="cardCvv = sanitizeCardCvv($event.target.value)"
                  />
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="summary-col">
          <div class="summary-card">
            <h2 class="summary-title">Ваш заказ</h2>

            <div class="mini-items-list">
              <div
                v-for="item in store.cart"
                :key="item.id"
                class="mini-item"
              >
                <div class="mini-item-thumb">
                  <img
                    :src="item.product.photo || '/images/default.png'"
                    :alt="item.product.title"
                    class="mini-thumb-img"
                    @error="e => e.target.src = '/images/default.png'"
                  />
                  <span class="thumb-qty-badge">{{ item.quantity }}</span>
                </div>

                <div class="mini-item-info">
                  <span class="mini-item-title">{{ item.product.title }}</span>
                  <span class="mini-item-price">
                    {{ (item.displayPrice * item.quantity).toLocaleString('ru-RU') }} ₽
                  </span>
                </div>
              </div>
            </div>

            <div class="divider" />

            <div class="calc-row">
              <span class="calc-label">Товары ({{ store.getCartCount() }})</span>
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
              <span class="total-label">Итого к оплате</span>
              <span class="total-value">{{ store.getCartTotal().toLocaleString('ru-RU') }} ₽</span>
            </div>

            <div v-if="error" class="error-notice" style="margin-top: 16px; margin-bottom: 0;">
              {{ error }}
            </div>

            <button
              type="submit"
              class="place-order-btn"
              :disabled="isSubmitting"
              :style="{ opacity: isSubmitting ? 0.7 : 1, cursor: isSubmitting ? 'not-allowed' : 'pointer' }"
            >
              {{ isSubmitting ? 'Оформление заказа...' : 'Оформить заказ' }}
            </button>
          </div>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import Header from '../components/Header.vue'
import { store } from '../store.js'
import {
  handleSpaceKeydown,
  sanitizeName,
  formatPhone,
  isValidPhone,
  sanitizeTextWithSpaces,
  sanitizeDigits,
  sanitizeCardNumber,
  sanitizeCardExpiry,
  sanitizeCardCvv
} from '../utils/validators.js'

const router = useRouter()

const name = ref(sanitizeName(store.user?.name || '', 2, 60))
const phone = ref(formatPhone(store.user?.phone || ''))
const city = ref('г. Уфа')
const street = ref('ул. Кирова')
const house = ref('д. 65/2')
const floor = ref('2')
const entrance = ref('1')
const apartment = ref('кв. 1')

const paymentMethod = ref('card')
const cardNumber = ref('4276 5500 1234 5678')
const cardExpiry = ref('12/28')
const cardCvv = ref('777')

const error = ref('')
const isSubmitting = ref(false)

async function submitOrder() {
  if (isSubmitting.value) return
  if (!name.value || !phone.value || !city.value || !street.value || !house.value) {
    error.value = 'Заполните поля доставки (ФИО, телефон, город, улица, дом)'
    return
  }

  if (!isValidPhone(phone.value)) {
    error.value = 'Введите полный номер телефона (+7 (XXX) XXX-XX-XX)'
    return
  }

  if (paymentMethod.value === 'card') {
    const rawCard = cardNumber.value.replace(/\s/g, '')
    if (rawCard.length !== 16) {
      error.value = 'Введите полный 16-значный номер карты'
      return
    }
    if (cardExpiry.value.length !== 5) {
      error.value = 'Введите срок действия карты (ММ/ГГ)'
      return
    }
    if (cardCvv.value.length !== 3) {
      error.value = 'Введите 3 цифры CVV кода'
      return
    }
  }

  isSubmitting.value = true
  error.value = ''
  try {
    const fullAddress = city.value + ', ' + street.value + ', ' + house.value + (apartment.value ? ', ' + apartment.value : '')
    const newOrder = await store.createOrder(fullAddress, name.value, phone.value)
    if (newOrder && newOrder.receiptId) {
      router.push('/receipt/' + newOrder.receiptId)
    } else {
      router.push('/profile?tab=history')
    }
  } catch (err) {
    console.error('Ошибка создания заказа:', err)
    error.value = err.message || 'Ошибка оформления заказа'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<style scoped>
.container {
  min-height: 100vh;
  background-color: var(--theme-bg);
  display: flex;
  flex-direction: column;
}

.checkout-wrapper {
  max-width: 1200px;
  width: 100%;
  margin: 0 auto;
  padding: 40px 40px 80px 40px;
}

.title {
  font-size: 32px;
  font-weight: 800;
  color: var(--theme-text-primary);
  margin-bottom: 30px;
}

.checkout-grid {
  display: grid;
  grid-template-columns: 1fr 380px;
  gap: 40px;
  align-items: flex-start;
}

.form-col {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.section-card {
  background-color: var(--theme-card-bg);
  border: 1px solid var(--theme-border);
  border-radius: 16px;
  padding: 28px;
}

.card-title {
  font-size: 18px;
  font-weight: 700;
  color: var(--theme-text-primary);
  margin-bottom: 20px;
}

.two-col-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
}

.four-col-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  margin-top: 16px;
}

.input-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.label {
  font-size: 13px;
  font-weight: 600;
  color: var(--theme-text-secondary);
}

.input {
  background-color: var(--theme-textbox-bg);
  border: 1px solid var(--theme-border);
  color: var(--theme-text-primary);
  border-radius: 8px;
  padding: 12px 14px;
  font-size: 14px;
  transition: border-color 0.15s ease;
}

.input:focus {
  border-color: var(--theme-accent);
}

.payment-methods {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-bottom: 20px;
}

.payment-option {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px 16px;
  border: 1px solid var(--theme-border);
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.15s ease;
}

.payment-option:hover {
  background-color: var(--theme-panel-bg);
}

.payment-option-active {
  border-color: var(--theme-accent);
  background-color: var(--theme-panel-bg);
}

.radio {
  accent-color: var(--theme-accent);
}

.payment-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--theme-text-primary);
}

.card-inputs {
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px solid var(--theme-border);
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.summary-col {
  display: flex;
  flex-direction: column;
}

.summary-card {
  background-color: var(--theme-card-bg);
  border: 1px solid var(--theme-border);
  border-radius: 16px;
  padding: 28px;
  position: sticky;
  top: 100px;
}

.summary-title {
  font-size: 20px;
  font-weight: 700;
  color: var(--theme-text-primary);
  margin-bottom: 20px;
}

.mini-items-list {
  display: flex;
  flex-direction: column;
  gap: 14px;
  max-height: 280px;
  overflow-y: auto;
}

.mini-item {
  display: flex;
  align-items: center;
  gap: 14px;
}

.mini-item-thumb {
  position: relative;
  width: 50px;
  height: 50px;
  border-radius: 8px;
  background-color: var(--theme-panel-bg);
  flex-shrink: 0;
}

.mini-thumb-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 8px;
}

.thumb-qty-badge {
  position: absolute;
  top: -6px;
  right: -6px;
  width: 18px;
  height: 18px;
  background-color: var(--theme-accent);
  color: #ffffff;
  border-radius: 4px;
  font-size: 10px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
}

.mini-item-info {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.mini-item-title {
  font-size: 14px;
  font-weight: 700;
  color: var(--theme-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.mini-item-price {
  font-size: 13px;
  font-weight: 600;
  color: var(--theme-text-secondary);
  margin-top: 2px;
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
  margin-bottom: 10px;
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
  margin-bottom: 20px;
}

.total-label {
  font-size: 16px;
  font-weight: 700;
  color: var(--theme-text-primary);
}

.total-value {
  font-size: 22px;
  font-weight: 800;
  color: var(--theme-text-primary);
}

.place-order-btn {
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

.place-order-btn:hover {
  background-color: var(--theme-accent-hover);
}

.error-notice {
  background-color: var(--theme-danger-bg);
  color: var(--theme-danger);
  padding: 12px 16px;
  border-radius: 8px;
  font-size: 14px;
}

.empty-state {
  text-align: center;
  padding: 60px 20px;
  background-color: var(--theme-card-bg);
  border-radius: 16px;
  border: 1px solid var(--theme-border);
}

.catalog-btn {
  margin-top: 20px;
  padding: 10px 24px;
  background-color: var(--theme-accent);
  color: #fff;
  border-radius: 8px;
  font-weight: 600;
}

@media (max-width: 900px) {
  .checkout-wrapper {
    padding: 20px 16px;
  }
  .checkout-grid {
    grid-template-columns: 1fr;
  }
  .four-col-grid {
    grid-template-columns: 1fr 1fr;
  }
  .summary-card {
    position: static;
  }
}
</style>
