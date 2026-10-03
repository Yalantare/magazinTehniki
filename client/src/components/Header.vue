<template>
  <header class="header">
    <router-link to="/catalog" class="left-section">
      <img src="/images/icon.png" alt="МагазинТехники" class="logo-icon" />
      <span class="logo-title">МагазинТехники</span>
    </router-link>

    <nav class="center-nav">
      <router-link to="/catalog" class="nav-link" active-class="nav-link-active">
        Каталог
      </router-link>

      <router-link
        v-if="!store.isGuest"
        to="/cart"
        class="nav-link"
        active-class="nav-link-active"
      >
        Корзина
        <span v-if="store.getCartCount() > 0" class="badge">{{ store.getCartCount() }}</span>
      </router-link>
    </nav>

    <div class="right-section">
      <button
        class="theme-toggle"
        @click="store.toggleTheme()"
        :title="store.theme === 'dark' ? 'Включить светлую тему' : 'Включить темную тему'"
      >
        <Sun v-if="store.theme === 'dark'" :size="20" />
        <Moon v-else :size="20" />
      </button>

      <button
        v-if="store.isGuest || !store.user"
        class="guest-login-btn"
        @click="router.push('/')"
      >
        Войти
      </button>

      <div
        v-else
        class="profile-panel"
        @click="router.push('/profile')"
        title="Перейти в личный кабинет"
      >
        <span class="user-name">{{ store.user?.name || 'Пользователь' }}</span>
        <div class="avatar-box">
          {{ getInitials(store.user?.name) }}
        </div>
      </div>
    </div>
  </header>
</template>

<script setup>
import { useRouter } from 'vue-router'
import { Sun, Moon } from 'lucide-vue-next'
import { store } from '../store.js'

const router = useRouter()

const getInitials = (name) => {
  if (!name || name.trim().length === 0) return 'П'
  return name.trim()[0].toUpperCase()
}
</script>

<style scoped>
.header {
  height: 70px;
  background-color: var(--theme-header-bg);
  border-bottom: 1px solid var(--theme-border);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 40px;
  position: sticky;
  top: 0;
  z-index: 100;
  backdrop-filter: blur(8px);
}

.left-section {
  display: flex;
  align-items: center;
  gap: 12px;
  cursor: pointer;
  text-decoration: none;
}

.logo-icon {
  height: 38px;
  width: auto;
  object-fit: contain;
}

.logo-title {
  font-size: 20px;
  font-weight: 700;
  color: var(--theme-text-primary);
  letter-spacing: -0.5px;
}

.center-nav {
  display: flex;
  align-items: center;
  gap: 20px;
}

.nav-link {
  color: var(--theme-text-secondary);
  font-size: 16px;
  font-weight: 500;
  text-decoration: none;
  padding: 8px 16px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  gap: 6px;
  transition: color 0.15s ease, background-color 0.15s ease;
}

.nav-link:hover {
  color: var(--theme-text-primary);
  background-color: var(--theme-panel-bg);
  text-decoration: none;
}

.nav-link-active {
  color: var(--theme-accent) !important;
}

.badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background-color: var(--theme-accent);
  color: #ffffff;
  font-size: 11px;
  font-weight: 700;
  border-radius: 4px;
  padding: 2px 7px;
  margin-left: 6px;
  vertical-align: middle;
}

.right-section {
  display: flex;
  align-items: center;
  gap: 15px;
}

.theme-toggle {
  background: transparent;
  border: none;
  color: var(--theme-text-secondary);
  display: flex;
  align-items: center;
  justify-content: center;
  width: 38px;
  height: 38px;
  border-radius: 8px;
  cursor: pointer;
  transition: color 0.15s, background-color 0.15s;
}

.theme-toggle:hover {
  color: var(--theme-text-primary);
  background-color: var(--theme-panel-bg);
}

.profile-panel {
  display: flex;
  align-items: center;
  gap: 12px;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 8px;
  transition: background-color 0.15s;
}

.profile-panel:hover {
  background-color: var(--theme-panel-bg);
}

.user-name {
  font-size: 16px;
  font-weight: 500;
  color: var(--theme-text-primary);
}

.avatar-box {
  width: 36px;
  height: 36px;
  border-radius: 8px;
  background-color: var(--theme-accent);
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  font-weight: 700;
  user-select: none;
}

.guest-login-btn {
  background-color: var(--theme-accent);
  color: #ffffff;
  font-size: 15px;
  font-weight: 700;
  padding: 8px 24px;
  border-radius: 8px;
  cursor: pointer;
  transition: background-color 0.15s;
}

.guest-login-btn:hover {
  background-color: var(--theme-accent-hover);
}

@media (max-width: 768px) {
  .header {
    padding: 0 16px;
  }
  .logo-title {
    display: none;
  }
  .user-name {
    display: none;
  }
}
</style>
