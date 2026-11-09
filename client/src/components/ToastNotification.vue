<template>
  <div class="toast-container" aria-live="polite" aria-atomic="true">
    <TransitionGroup name="toast-anim" tag="div" class="toast-list">
      <div
        v-for="toast in store.notifications"
        :key="toast.id"
        :class="['toast-card', `toast-${toast.type || 'info'}`]"
        role="alert"
      >
        <div class="toast-icon-wrapper">
          <AlertCircle v-if="toast.type === 'error'" :size="20" class="toast-icon" />
          <AlertTriangle v-else-if="toast.type === 'warning'" :size="20" class="toast-icon" />
          <CheckCircle2 v-else-if="toast.type === 'success'" :size="20" class="toast-icon" />
          <Info v-else :size="20" class="toast-icon" />
        </div>

        <div class="toast-content">
          <div v-if="toast.title" class="toast-title">{{ toast.title }}</div>
          <div class="toast-message">{{ toast.message }}</div>
        </div>

        <button
          type="button"
          class="toast-close-btn"
          aria-label="Закрыть уведомление"
          @click="store.removeNotification(toast.id)"
        >
          <X :size="16" />
        </button>
      </div>
    </TransitionGroup>
  </div>
</template>

<script setup>
import { AlertCircle, AlertTriangle, CheckCircle2, Info, X } from 'lucide-vue-next'
import { store } from '../store.js'
</script>

<style scoped>
.toast-container {
  position: fixed;
  top: 24px;
  right: 24px;
  z-index: 10000;
  display: flex;
  flex-direction: column;
  pointer-events: none;
  max-width: 440px;
  width: calc(100vw - 32px);
}

.toast-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.toast-card {
  pointer-events: auto;
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 14px 16px;
  border-radius: 12px;
  background-color: var(--theme-card-bg);
  color: var(--theme-text-primary);
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.45), 0 1px 3px rgba(0, 0, 0, 0.2);
  border: 1px solid var(--theme-border);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}

.toast-warning {
  border-left: 4px solid var(--theme-warning);
  background: linear-gradient(135deg, rgba(245, 158, 11, 0.12) 0%, var(--theme-card-bg) 100%);
}

.toast-warning .toast-icon {
  color: var(--theme-warning);
}

.toast-error {
  border-left: 4px solid var(--theme-danger);
  background: linear-gradient(135deg, rgba(239, 68, 68, 0.12) 0%, var(--theme-card-bg) 100%);
}

.toast-error .toast-icon {
  color: var(--theme-danger);
}

.toast-success {
  border-left: 4px solid var(--theme-success);
  background: linear-gradient(135deg, rgba(16, 185, 129, 0.12) 0%, var(--theme-card-bg) 100%);
}

.toast-success .toast-icon {
  color: var(--theme-success);
}

.toast-info {
  border-left: 4px solid var(--theme-accent);
  background: linear-gradient(135deg, rgba(37, 99, 235, 0.12) 0%, var(--theme-card-bg) 100%);
}

.toast-info .toast-icon {
  color: var(--theme-accent);
}

.toast-icon-wrapper {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-top: 1px;
}

.toast-content {
  flex: 1;
  min-width: 0;
}

.toast-title {
  font-size: 14px;
  font-weight: 700;
  margin-bottom: 2px;
  color: var(--theme-text-primary);
}

.toast-message {
  font-size: 13px;
  line-height: 1.45;
  color: var(--theme-text-primary);
  word-break: break-word;
}

.toast-close-btn {
  flex-shrink: 0;
  background: transparent;
  color: var(--theme-text-muted);
  padding: 4px;
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: color 0.15s, background-color 0.15s;
  margin-top: -2px;
  margin-right: -4px;
}

.toast-close-btn:hover {
  color: var(--theme-text-primary);
  background-color: rgba(255, 255, 255, 0.08);
}

/* Animations */
.toast-anim-enter-active,
.toast-anim-leave-active {
  transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.toast-anim-enter-from {
  opacity: 0;
  transform: translateX(40px) scale(0.95);
}

.toast-anim-leave-to {
  opacity: 0;
  transform: translateX(30px) scale(0.9);
}

@media (max-width: 640px) {
  .toast-container {
    top: 12px;
    right: 12px;
    left: 12px;
    width: auto;
  }
}
</style>
