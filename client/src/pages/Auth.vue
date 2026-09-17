<template>
  <div class="container">
    <div class="hero-banner">
      <div class="hero-content">
        <div class="logo-row">
          <img src="/images/icon.png" alt="МагазинТехники" class="hero-logo-icon" />
          <span class="hero-logo-text">МагазинТехники</span>
        </div>

        <h1 class="hero-title">Будущее<br />магазинов электроники</h1>

        <p class="hero-subtitle">
          Получите доступ к новейшим гаджетам и полной истории ваших заказов - все это в одном месте.
        </p>

        <div class="benefits-list">
          <div class="benefit-item">- Актуальный каталог</div>
          <div class="benefit-item">- Безопасный заказ</div>
          <div class="benefit-item">- Доставка в кратчайшие сроки</div>
        </div>
      </div>
    </div>

    <div class="form-section">
      <div class="form-box">
        <form v-if="!isRegister" @submit.prevent="submitLogin">
          <h2 class="title">Добро пожаловать</h2>
          <p class="subtitle">Войдите в аккаунт или продолжите как гость</p>

          <div v-if="error" class="error-notice">
            {{ error }}
          </div>

          <div class="input-group">
            <label class="label">Email или номер телефона</label>
            <input
              v-model="loginEmail"
              type="text"
              placeholder="user@example.com"
              class="input"
            />
          </div>

          <div class="input-group">
            <label class="label">Пароль</label>
            <div class="pass-wrapper">
              <input
                v-model="loginPass"
                :type="showPassword ? 'text' : 'password'"
                placeholder="Введите пароль"
                class="input"
              />
              <button
                type="button"
                class="eye-btn"
                @click="showPassword = !showPassword"
              >
                <EyeOff v-if="showPassword" :size="18" />
                <Eye v-else :size="18" />
              </button>
            </div>
          </div>

          <button type="submit" class="submit-btn">
            Войти в аккаунт
          </button>

          <div class="divider-row">
            <span class="divider-line" />
            <span class="divider-text">или быстрый доступ</span>
            <span class="divider-line" />
          </div>

          <div class="demo-buttons">
            <button
              type="button"
              class="demo-btn"
              @click="loginDemo"
            >
              Войти как Клиент (демо)
            </button>
            <button
              type="button"
              class="guest-btn"
              @click="loginGuest"
            >
              Продолжить как Гость
            </button>
          </div>

          <div class="footer-link">
            Нет учетной записи?
            <button
              type="button"
              class="switch-btn"
              @click="isRegister = true; error = ''"
            >
              Зарегистрироваться
            </button>
          </div>
        </form>

        <form v-else @submit.prevent="submitRegister">
          <h2 class="title">Регистрация</h2>
          <p class="subtitle">Создайте аккаунт для покупок</p>

          <div v-if="error" class="error-notice">
            {{ error }}
          </div>

          <div class="input-group">
            <label class="label">Ваше имя *</label>
            <input
              v-model="regName"
              type="text"
              placeholder="Иван Иванов"
              class="input"
            />
          </div>

          <div class="input-group">
            <label class="label">Email *</label>
            <input
              v-model="regEmail"
              type="email"
              placeholder="ivanov@example.com"
              class="input"
            />
          </div>

          <div class="input-group">
            <label class="label">Телефон *</label>
            <input
              v-model="regPhone"
              type="tel"
              placeholder="+7 (999) 000-00-00"
              class="input"
            />
          </div>

          <div class="input-group">
            <label class="label">Пароль *</label>
            <div class="pass-wrapper">
              <input
                v-model="regPass"
                :type="showPassword ? 'text' : 'password'"
                placeholder="Придумайте пароль"
                class="input"
              />
              <button
                type="button"
                class="eye-btn"
                @click="showPassword = !showPassword"
              >
                <EyeOff v-if="showPassword" :size="18" />
                <Eye v-else :size="18" />
              </button>
            </div>
          </div>

          <div class="checkbox-row">
            <input
              v-model="agree"
              type="checkbox"
              id="agree"
              class="checkbox"
            />
            <label for="agree" class="checkbox-label">
              Я соглашаюсь с условиями обработки персональных данных
            </label>
          </div>

          <button type="submit" class="submit-btn">
            Зарегистрироваться
          </button>

          <div class="footer-link">
            Уже зарегистрированы?
            <button
              type="button"
              class="switch-btn"
              @click="isRegister = false; error = ''"
            >
              Войти
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { Eye, EyeOff } from 'lucide-vue-next'
import { store } from '../store.js'

const router = useRouter()
const isRegister = ref(false)
const error = ref('')
const showPassword = ref(false)

const loginEmail = ref('hayrullinrafael2@gmail.com')
const loginPass = ref('password123')

const regName = ref('')
const regEmail = ref('')
const regPhone = ref('')
const regPass = ref('')
const agree = ref(false)

function submitLogin() {
  if (!loginEmail.value || !loginPass.value) {
    error.value = 'Заполните поля'
    return
  }
  store.login(loginEmail.value, loginPass.value)
  router.push('/catalog')
}

function loginDemo() {
  store.login('hayrullinrafael2@gmail.com', 'password123')
  router.push('/catalog')
}

function loginGuest() {
  store.loginGuest()
  router.push('/catalog')
}

function submitRegister() {
  if (!regName.value || !regEmail.value || !regPhone.value || !regPass.value) {
    error.value = 'Заполните все поля'
    return
  }
  if (!agree.value) {
    error.value = 'Подтвердите согласие'
    return
  }
  store.register(regName.value, regEmail.value, regPhone.value, regPass.value)
  router.push('/catalog')
}
</script>

<style scoped>
.container {
  display: grid;
  grid-template-columns: 1.2fr 1fr;
  min-height: 100vh;
  background-color: var(--theme-bg);
}

.hero-banner {
  background: var(--theme-login-gradient);
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding: 60px;
}

.hero-content {
  max-width: 550px;
}

.logo-row {
  display: flex;
  align-items: center;
  gap: 15px;
  margin-bottom: 60px;
}

.hero-logo-icon {
  height: 55px;
  width: auto;
  object-fit: contain;
}

.hero-logo-text {
  font-size: 24px;
  font-weight: 700;
  color: #ffffff;
}

.hero-title {
  font-size: 40px;
  font-weight: 700;
  color: #ffffff;
  line-height: 1.2;
  margin-bottom: 25px;
}

.hero-subtitle {
  font-size: 18px;
  color: var(--theme-text-secondary);
  line-height: 1.6;
  margin-bottom: 50px;
}

.benefits-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.benefit-item {
  font-size: 16px;
  color: #ffffff;
  display: flex;
  align-items: center;
  gap: 10px;
}

.form-section {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 60px;
  overflow-y: auto;
}

.form-box {
  width: 100%;
  max-width: 400px;
}

.title {
  font-size: 32px;
  font-weight: 700;
  color: var(--theme-text-primary);
  margin-bottom: 8px;
}

.subtitle {
  font-size: 15px;
  color: var(--theme-text-muted);
  margin-bottom: 35px;
}

.error-notice {
  background-color: var(--theme-danger-bg);
  color: var(--theme-danger);
  padding: 12px 16px;
  border-radius: 8px;
  margin-bottom: 20px;
  font-size: 14px;
}

.input-group {
  margin-bottom: 16px;
}

.label {
  display: block;
  font-size: 13px;
  font-weight: 600;
  color: var(--theme-text-secondary);
  margin-bottom: 6px;
}

.input {
  width: 100%;
  background-color: var(--theme-textbox-bg);
  border: 1px solid var(--theme-border);
  color: var(--theme-text-primary);
  border-radius: 8px;
  padding: 12px 14px;
  font-size: 15px;
  transition: border-color 0.15s ease;
}

.input:focus {
  border-color: var(--theme-accent);
}

.pass-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.eye-btn {
  position: absolute;
  right: 12px;
  background: transparent;
  color: var(--theme-text-muted);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 4px;
}

.eye-btn:hover {
  color: var(--theme-text-primary);
}

.submit-btn {
  width: 100%;
  background-color: var(--theme-accent);
  color: #ffffff;
  padding: 14px;
  border-radius: 8px;
  font-size: 16px;
  font-weight: 700;
  cursor: pointer;
  margin-top: 10px;
  transition: background-color 0.15s ease;
}

.submit-btn:hover {
  background-color: var(--theme-accent-hover);
}

.divider-row {
  display: flex;
  align-items: center;
  gap: 12px;
  margin: 24px 0 16px 0;
}

.divider-line {
  flex: 1;
  height: 1px;
  background-color: var(--theme-border);
}

.divider-text {
  font-size: 12px;
  color: var(--theme-text-muted);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.demo-buttons {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.demo-btn {
  width: 100%;
  background-color: var(--theme-panel-bg);
  border: 1px solid var(--theme-border);
  color: var(--theme-text-primary);
  padding: 12px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.15s, border-color 0.15s;
}

.demo-btn:hover {
  background-color: var(--theme-textbox-bg);
  border-color: var(--theme-border-light);
}

.guest-btn {
  width: 100%;
  background-color: transparent;
  border: 1px dashed var(--theme-border-light);
  color: var(--theme-text-secondary);
  padding: 12px;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.15s;
}

.guest-btn:hover {
  color: var(--theme-text-primary);
  border-color: var(--theme-accent);
}

.footer-link {
  margin-top: 24px;
  text-align: center;
  font-size: 14px;
  color: var(--theme-text-secondary);
}

.switch-btn {
  background: transparent;
  color: var(--theme-accent);
  font-weight: 600;
  margin-left: 6px;
  padding: 0;
}

.switch-btn:hover {
  text-decoration: underline;
}

.checkbox-row {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  margin-bottom: 20px;
}

.checkbox {
  margin-top: 3px;
  accent-color: var(--theme-accent);
}

.checkbox-label {
  font-size: 13px;
  color: var(--theme-text-secondary);
  line-height: 1.4;
  cursor: pointer;
}

@media (max-width: 900px) {
  .container {
    grid-template-columns: 1fr;
  }
  .hero-banner {
    display: none;
  }
  .form-section {
    padding: 30px 20px;
  }
}
</style>
