<template>
  <div class="container">
    <Header />

    <div v-if="store.isGuest || !store.user" class="guest-notice">
      <h2>Личный кабинет недоступен в гостевом режиме</h2>
      <p style="margin-top: 10px; color: var(--theme-text-secondary);">
        Пожалуйста, войдите в свой профиль или зарегистрируйтесь.
      </p>
      <button class="login-btn" @click="router.push('/')">
        Войти
      </button>
    </div>

    <div v-else class="profile-wrapper">
      <div class="profile-grid">
        <aside class="sidebar">
          <div class="avatar-card">
            <div class="large-avatar-box">
              {{ (store.user?.name || 'П')[0].toUpperCase() }}
            </div>
            <h2 class="sidebar-user-name">{{ store.user?.name || 'Пользователь' }}</h2>
          </div>

          <div class="stats-card">
            <h3 class="stats-title">Статистика заказов</h3>
            <div class="stats-list">
              <div class="stat-item">
                <span class="stat-label">Всего заказов</span>
                <span class="stat-value">{{ userOrders.length }}</span>
              </div>
              <div class="stat-item">
                <span class="stat-label">Сумма покупок</span>
                <span class="stat-value">{{ totalSpent.toLocaleString('ru-RU') }} ₽</span>
              </div>
            </div>
          </div>

          <button class="logout-btn" @click="logout">
            Выйти из аккаунта
          </button>
        </aside>

        <main class="main-content-panel">
          <div class="tabs-nav">
            <button
              :class="['tab-btn', activeTab === 'personal' ? 'tab-btn-active' : '']"
              @click="activeTab = 'personal'"
            >
              Личные данные
            </button>
            <button
              :class="['tab-btn', activeTab === 'history' ? 'tab-btn-active' : '']"
              @click="activeTab = 'history'"
            >
              История заказов ({{ userOrders.length }})
            </button>
          </div>

          <div v-if="activeTab === 'personal'" class="tab-pane">
            <div class="pane-header">
              <div>
                <h2 class="pane-title">Информация профиля</h2>
                <p class="pane-subtitle">Ваши контактные данные для оформления заказов</p>
              </div>
              <button
                v-if="!isEditing"
                class="edit-toggle-btn"
                @click="startEdit"
              >
                Редактировать
              </button>
            </div>

            <form v-if="isEditing" @submit.prevent="saveProfile" class="edit-form">
              <div class="form-group">
                <label class="form-label">ФИО</label>
                <input
                  v-model="editName"
                  type="text"
                  class="form-input"
                  required
                />
              </div>

              <div class="form-group">
                <label class="form-label">Телефон</label>
                <input
                  v-model="editPhone"
                  type="tel"
                  class="form-input"
                  required
                />
              </div>

              <div class="form-group">
                <label class="form-label">Email</label>
                <input
                  v-model="editEmail"
                  type="email"
                  class="form-input"
                  required
                />
              </div>

              <div class="form-group">
                <label class="form-label">Пароль</label>
                <input
                  v-model="editPassword"
                  type="text"
                  class="form-input"
                  required
                />
              </div>

              <div class="edit-actions">
                <button type="button" class="cancel-btn" @click="isEditing = false">
                  Отмена
                </button>
                <button type="submit" class="save-btn">
                  Сохранить
                </button>
              </div>
            </form>

            <div v-else class="view-grid">
              <div class="field-box">
                <span class="field-label">ФИО</span>
                <span class="field-value">{{ store.user.name }}</span>
              </div>

              <div class="field-box">
                <span class="field-label">Телефон</span>
                <span class="field-value">{{ store.user.phone }}</span>
              </div>

              <div class="field-box">
                <span class="field-label">Email</span>
                <span class="field-value">{{ store.user.email || 'Не указан' }}</span>
              </div>

              <div class="field-box">
                <span class="field-label">Пароль</span>
                <span class="field-value">********</span>
              </div>
            </div>
          </div>

          <div v-else class="tab-pane">
            <div class="pane-header">
              <div>
                <h2 class="pane-title">История заказов</h2>
                <p class="pane-subtitle">Все оформленные вами заказы и квитанции</p>
              </div>
            </div>

            <div v-if="userOrders.length > 0" class="orders-list">
              <div
                v-for="order in userOrders"
                :key="order.receiptId"
                class="order-card"
              >
                <div class="order-card-header">
                  <div>
                    <span class="order-code">{{ order.code }}</span>
                    <span class="order-date">{{ order.dateTime ? new Date(order.dateTime).toLocaleDateString('ru-RU') : '' }}</span>
                  </div>
                  <span class="order-status-tag">{{ order.statusTitle }}</span>
                </div>

                <div class="order-items-preview">
                  <div
                    v-for="item in order.receiptItems"
                    :key="item.id"
                    class="order-item-row"
                  >
                    <span>
                      {{ item.product?.title || 'Товар' }} x {{ item.quantity }}
                    </span>
                    <span class="order-item-price">
                      {{ (item.priceAtPurchase * item.quantity).toLocaleString('ru-RU') }} ₽
                    </span>
                  </div>
                </div>

                <div class="order-card-footer">
                  <span class="order-total">
                    Сумма: <strong>{{ order.totalPrice.toLocaleString('ru-RU') }} ₽</strong>
                  </span>
                  <button
                    class="view-receipt-btn"
                    @click="router.push('/receipt/' + order.receiptId)"
                  >
                    Посмотреть чек
                  </button>
                </div>
              </div>
            </div>

            <div v-else class="no-orders">
              У вас пока нет оформленных заказов.
            </div>
          </div>
        </main>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Header from '../components/Header.vue'
import { store } from '../store.js'

const route = useRoute()
const router = useRouter()

onMounted(async () => {
  await store.loadOrders()
})

const activeTab = ref(route.query.tab === 'history' ? 'history' : 'personal')
const isEditing = ref(false)

const editName = ref('')
const editPhone = ref('')
const editEmail = ref('')
const editPassword = ref('')

const userOrders = computed(() => {
  if (!store.user) return []
  return store.orders.filter(o => o.userId === store.user.userId)
})

const totalSpent = computed(() => {
  let sum = 0
  for (let i = 0; i < userOrders.value.length; i++) {
    sum += userOrders.value[i].totalPrice
  }
  return sum
})

function startEdit() {
  editName.value = store.user.name
  editPhone.value = store.user.phone
  editEmail.value = store.user.email || ''
  editPassword.value = store.user.password || 'password123'
  isEditing.value = true
}

function saveProfile() {
  store.updateUser({
    name: editName.value,
    phone: editPhone.value,
    email: editEmail.value,
    password: editPassword.value
  })
  isEditing.value = false
}

function logout() {
  store.logout()
  router.push('/')
}
</script>

<style scoped>
.container {
  min-height: 100vh;
  background-color: var(--theme-bg);
  display: flex;
  flex-direction: column;
}

.guest-notice {
  text-align: center;
  padding: 60px 20px;
}

.login-btn {
  margin-top: 20px;
  padding: 10px 24px;
  background-color: var(--theme-accent);
  color: #fff;
  border-radius: 8px;
  font-weight: 600;
}

.profile-wrapper {
  max-width: 1200px;
  width: 100%;
  margin: 0 auto;
  padding: 40px 40px 80px 40px;
}

.profile-grid {
  display: grid;
  grid-template-columns: 300px 1fr;
  gap: 40px;
  align-items: flex-start;
}

.sidebar {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.avatar-card {
  background-color: var(--theme-card-bg);
  border: 1px solid var(--theme-border);
  border-radius: 12px;
  padding: 30px 20px;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.large-avatar-box {
  width: 70px;
  height: 70px;
  border-radius: 12px;
  background-color: var(--theme-accent);
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 28px;
  font-weight: 700;
  margin-bottom: 15px;
}

.sidebar-user-name {
  font-size: 18px;
  font-weight: 700;
  color: var(--theme-text-primary);
  text-align: center;
}

.stats-card {
  background-color: var(--theme-card-bg);
  border: 1px solid var(--theme-border);
  border-radius: 12px;
  padding: 24px;
}

.stats-title {
  font-size: 16px;
  font-weight: 700;
  color: var(--theme-text-primary);
  margin-bottom: 16px;
}

.stats-list {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.stat-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 14px;
}

.stat-label {
  color: var(--theme-text-secondary);
}

.stat-value {
  font-weight: 700;
  color: var(--theme-text-primary);
}

.logout-btn {
  width: 100%;
  background-color: var(--theme-card-bg);
  border: 1px solid var(--theme-border);
  color: var(--theme-danger);
  padding: 12px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s ease;
}

.logout-btn:hover {
  background-color: var(--theme-danger-bg);
}

.main-content-panel {
  background-color: var(--theme-card-bg);
  border: 1px solid var(--theme-border);
  border-radius: 16px;
  overflow: hidden;
}

.tabs-nav {
  display: flex;
  border-bottom: 1px solid var(--theme-border);
  background-color: var(--theme-header-bg);
}

.tab-btn {
  flex: 1;
  padding: 16px 20px;
  background: transparent;
  color: var(--theme-text-secondary);
  font-size: 15px;
  font-weight: 600;
  border-bottom: 2px solid transparent;
  cursor: pointer;
  transition: all 0.15s ease;
}

.tab-btn:hover {
  color: var(--theme-text-primary);
}

.tab-btn-active {
  color: var(--theme-accent) !important;
  border-bottom-color: var(--theme-accent) !important;
  background-color: var(--theme-card-bg);
}

.tab-pane {
  padding: 30px;
}

.pane-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 25px;
}

.pane-title {
  font-size: 20px;
  font-weight: 700;
  color: var(--theme-text-primary);
  margin-bottom: 4px;
}

.pane-subtitle {
  font-size: 14px;
  color: var(--theme-text-secondary);
}

.edit-toggle-btn {
  background-color: var(--theme-panel-bg);
  border: 1px solid var(--theme-border);
  color: var(--theme-text-primary);
  padding: 8px 16px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
}

.view-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
}

.field-box {
  background-color: var(--theme-panel-bg);
  border: 1px solid var(--theme-border);
  border-radius: 8px;
  padding: 14px 18px;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.field-label {
  font-size: 12px;
  font-weight: 600;
  color: var(--theme-text-muted);
}

.field-value {
  font-size: 15px;
  font-weight: 600;
  color: var(--theme-text-primary);
}

.edit-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
  max-width: 500px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.form-label {
  font-size: 13px;
  font-weight: 600;
  color: var(--theme-text-secondary);
}

.form-input {
  background-color: var(--theme-textbox-bg);
  border: 1px solid var(--theme-border);
  color: var(--theme-text-primary);
  border-radius: 8px;
  padding: 10px 14px;
  font-size: 14px;
}

.edit-actions {
  display: flex;
  gap: 12px;
  margin-top: 10px;
}

.cancel-btn {
  background-color: transparent;
  border: 1px solid var(--theme-border);
  color: var(--theme-text-secondary);
  padding: 10px 20px;
  border-radius: 8px;
  font-size: 14px;
  cursor: pointer;
}

.save-btn {
  background-color: var(--theme-accent);
  color: #ffffff;
  padding: 10px 20px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
}

.orders-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.order-card {
  background-color: var(--theme-panel-bg);
  border: 1px solid var(--theme-border);
  border-radius: 12px;
  padding: 20px;
}

.order-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 14px;
}

.order-code {
  font-size: 16px;
  font-weight: 700;
  color: var(--theme-text-primary);
  margin-right: 12px;
}

.order-date {
  font-size: 13px;
  color: var(--theme-text-muted);
}

.order-status-tag {
  background-color: rgba(16, 185, 129, 0.2);
  color: var(--theme-success);
  border: 1px solid var(--theme-success);
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 600;
}

.order-items-preview {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 12px 0;
  border-top: 1px solid var(--theme-border);
  border-bottom: 1px solid var(--theme-border);
  font-size: 14px;
  color: var(--theme-text-secondary);
}

.order-item-row {
  display: flex;
  justify-content: space-between;
}

.order-item-price {
  font-weight: 600;
  color: var(--theme-text-primary);
}

.order-card-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 14px;
}

.order-total {
  font-size: 14px;
  color: var(--theme-text-secondary);
}

.order-total strong {
  color: var(--theme-text-primary);
  font-size: 16px;
}

.view-receipt-btn {
  background-color: var(--theme-accent);
  color: #ffffff;
  padding: 8px 16px;
  border-radius: 6px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
}

.no-orders {
  text-align: center;
  padding: 40px;
  color: var(--theme-text-secondary);
  font-size: 15px;
}

@media (max-width: 900px) {
  .profile-grid {
    grid-template-columns: 1fr;
  }
  .view-grid {
    grid-template-columns: 1fr;
  }
}
</style>
