<template>
  <div class="chatbot-container">
    <!-- Плавающая кнопка открытия чата -->
    <button 
      v-if="!isOpen" 
      @click="toggleChat" 
      class="chat-toggle-btn"
      title="Онлайн-консультант"
    >
      <div class="toggle-icon-wrap">
        <Bot class="icon-toggle" />
      </div>
      <span class="online-badge-dot"></span>
    </button>

    <!-- Окно чата -->
    <div v-else class="chat-window">
      <!-- Хедер -->
      <div class="chat-header">
        <div class="header-info">
          <div class="bot-avatar-box">
            <Bot class="bot-avatar-icon" />
          </div>
          <div class="header-titles">
            <h3 class="header-title">Консультант</h3>
            <div class="status-row">
              <span class="status-dot"></span>
              <span class="status-text">Онлайн &bull; ТехноМир</span>
            </div>
          </div>
        </div>

        <div class="header-actions">
          <button @click="clearHistory" class="header-action-btn" title="Очистить историю">
            <Trash2 class="action-icon" />
          </button>
          <button @click="toggleChat" class="header-action-btn" title="Закрыть">
            <X class="action-icon" />
          </button>
        </div>
      </div>

      <!-- Область сообщений -->
      <div class="messages-container" ref="messagesContainer">
        <!-- Сообщения -->
        <div 
          v-for="(msg, index) in messages" 
          :key="index" 
          :class="['message-row', msg.role === 'user' ? 'message-user' : 'message-bot']"
        >
          <div class="message-bubble">
            <div class="message-text" v-html="renderMarkdown(msg.content)"></div>
            <span class="message-time">{{ msg.time || 'Сейчас' }}</span>
          </div>
        </div>

        <!-- Подсказки (suggestions chips), как на примере фото -->
        <div v-if="messages.length <= 1" class="suggestions-grid">
          <button
            v-for="(sug, idx) in suggestions"
            :key="idx"
            class="suggestion-chip"
            @click="sendQuickMessage(sug.query)"
          >
            <span class="chip-sparkle">&bull;&bull;&bull;</span>
            <span class="chip-icon">{{ sug.icon }}</span>
            <span class="chip-text">{{ sug.label }}</span>
          </button>
        </div>

        <!-- Индикатор набора текста / генерации ответа -->
        <div v-if="isLoading" class="message-row message-bot">
          <div class="typing-bubble">
            <span class="typing-dot"></span>
            <span class="typing-dot"></span>
            <span class="typing-dot"></span>
          </div>
        </div>
      </div>

      <!-- Панель ввода -->
      <form @submit.prevent="sendMessage" class="input-form">
        <input 
          ref="inputRef"
          v-model="inputText" 
          type="text" 
          placeholder="Напишите вопрос..." 
          :disabled="isLoading"
          class="chat-input"
        />
        <button 
          type="submit" 
          :disabled="isLoading || !inputText.trim()" 
          class="send-btn"
          title="Отправить"
        >
          <Send class="send-icon" />
        </button>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref, nextTick, onMounted } from 'vue';
import { X, Send, Bot, Trash2 } from 'lucide-vue-next';
import { marked } from 'marked';
import { api } from '../api';

// Состояния
const isOpen = ref(false);
const inputText = ref('');
const isLoading = ref(false);
const messagesContainer = ref(null);
const inputRef = ref(null);

// Быстрые подсказки
const suggestions = [
  { icon: '📱', label: 'Какой смартфон выбрать?', query: 'Какой смартфон лучше выбрать?' },
  { icon: '🚚', label: 'Условия доставки', query: 'Расскажи про условия доставки и сроки' },
  { icon: '🛡️', label: 'Гарантия и возврат', query: 'Какая гарантия действует и как оформить возврат?' },
  { icon: '🎧', label: 'Лучшие наушники', query: 'Посоветуй лучшие наушники по соотношению цена-качество' },
  { icon: '💳', label: 'Как оплатить заказ?', query: 'Какие способы оплаты доступны в магазине?' }
];

const getCurrentTime = () => {
  const now = new Date();
  return now.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });
};

// Приветственное сообщение
const welcomeMessage = {
  role: 'assistant',
  content: 'Здравствуйте! Я онлайн-консультант магазина техники. 🤖 Чем могу помочь вам сегодня?',
  time: 'Сейчас'
};

const messages = ref([{ ...welcomeMessage }]);

// Открытие / закрытие
const toggleChat = () => {
  isOpen.value = !isOpen.value;
  if (isOpen.value) {
    scrollToBottom();
    setTimeout(() => {
      inputRef.value?.focus();
    }, 150);
  }
};

// Очистка истории с возвратом приветствия
const clearHistory = () => {
  messages.value = [{ ...welcomeMessage, time: getCurrentTime() }];
};

// Рендер Markdown
const renderMarkdown = (text) => {
  if (!text) return '';
  return marked.parse(text);
};

// Авто-скролл
const scrollToBottom = async () => {
  await nextTick();
  if (messagesContainer.value) {
    messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight;
  }
};

// Отправка быстрого вопроса из подсказок
const sendQuickMessage = (text) => {
  inputText.value = text;
  sendMessage();
};

// Отправка сообщения
const sendMessage = async () => {
  const text = inputText.value.trim();
  if (!text || isLoading.value) return;

  const userTime = getCurrentTime();
  messages.value.push({ role: 'user', content: text, time: userTime });
  inputText.value = '';
  isLoading.value = true;
  await scrollToBottom();

  try {
    // Подготовка истории для бэкенда
    const payload = messages.value.map(m => ({ role: m.role, content: m.content }));
    const data = await api.sendChatMessage(payload);

    messages.value.push({ 
      role: 'assistant', 
      content: data.reply,
      time: getCurrentTime()
    });
  } catch (error) {
    console.error('Ошибка чата:', error);
    messages.value.push({ 
      role: 'assistant', 
      content: `⚠️ **Не удалось получить ответ:** ${error.message || 'Ошибка соединения с сервером.'}`,
      time: getCurrentTime()
    });
  } finally {
    isLoading.value = false;
    await scrollToBottom();
  }
};

onMounted(() => {
  // Настройка marked
  marked.setOptions({
    gfm: true,
    breaks: true
  });
});
</script>

<style scoped>
/* Фиксированное положение виджета */
.chatbot-container {
  position: fixed;
  bottom: 24px;
  right: 24px;
  z-index: 9999;
  font-family: inherit;
}

/* Круглая кнопка открытия чата */
.chat-toggle-btn {
  position: relative;
  width: 58px;
  height: 58px;
  border-radius: 50%;
  background: linear-gradient(135deg, #1E3A8A 0%, var(--theme-accent) 100%);
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 8px 24px rgba(37, 99, 235, 0.45);
  cursor: pointer;
  border: 2px solid rgba(255, 255, 255, 0.2);
  transition: transform 0.25s cubic-bezier(0.175, 0.885, 0.32, 1.275), box-shadow 0.2s ease;
}

.chat-toggle-btn:hover {
  transform: scale(1.08) translateY(-2px);
  box-shadow: 0 12px 28px rgba(37, 99, 235, 0.6);
}

.toggle-icon-wrap {
  display: flex;
  align-items: center;
  justify-content: center;
}

.icon-toggle {
  width: 28px;
  height: 28px;
}

.online-badge-dot {
  position: absolute;
  top: 2px;
  right: 2px;
  width: 14px;
  height: 14px;
  border-radius: 50%;
  background-color: #10B981;
  border: 2.5px solid var(--theme-bg, #0A0E1A);
}

/* Окно чата */
.chat-window {
  width: 380px;
  height: 580px;
  max-height: calc(100vh - 48px);
  background-color: var(--theme-bg, #0A0E1A);
  border: 1px solid var(--theme-border-light, #334155);
  border-radius: 18px;
  display: flex;
  flex-direction: column;
  box-shadow: 0 20px 48px rgba(0, 0, 0, 0.45);
  overflow: hidden;
  animation: chatFadeIn 0.25s ease-out;
}

@keyframes chatFadeIn {
  from {
    opacity: 0;
    transform: translateY(16px) scale(0.96);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

/* Хедер */
.chat-header {
  background-color: var(--theme-header-bg, #050A18);
  border-bottom: 1px solid var(--theme-border, #1E293B);
  padding: 14px 18px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.header-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.bot-avatar-box {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: linear-gradient(135deg, #2563EB 0%, #3B82F6 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #ffffff;
  box-shadow: 0 2px 8px rgba(37, 99, 235, 0.35);
}

.bot-avatar-icon {
  width: 22px;
  height: 22px;
}

.header-titles {
  display: flex;
  flex-direction: column;
}

.header-title {
  font-size: 15px;
  font-weight: 700;
  color: var(--theme-text-primary, #ffffff);
  line-height: 1.2;
}

.status-row {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-top: 3px;
}

.status-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background-color: #10B981;
}

.status-text {
  font-size: 12px;
  color: #10B981;
  font-weight: 500;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 6px;
}

.header-action-btn {
  background: transparent;
  color: var(--theme-text-secondary, #94A3B8);
  width: 32px;
  height: 32px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.15s ease;
}

.header-action-btn:hover {
  background-color: var(--theme-panel-bg, #1E293B);
  color: var(--theme-text-primary, #ffffff);
}

.action-icon {
  width: 18px;
  height: 18px;
}

/* Область сообщений */
.messages-container {
  flex: 1;
  padding: 16px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 14px;
  background-color: var(--theme-bg, #0A0E1A);
}

/* Строка сообщения */
.message-row {
  display: flex;
  flex-direction: column;
  max-width: 86%;
}

.message-bot {
  align-self: flex-start;
}

.message-user {
  align-self: flex-end;
}

/* Пузырь сообщения */
.message-bubble {
  padding: 12px 16px;
  border-radius: 14px;
  font-size: 13.5px;
  line-height: 1.55;
  word-break: break-word;
}

.message-bot .message-bubble {
  background-color: var(--theme-card-bg, #111827);
  border: 1px solid var(--theme-border, #1E293B);
  color: var(--theme-text-primary, #ffffff);
  border-bottom-left-radius: 4px;
  box-shadow: var(--theme-card-shadow, 0 4px 12px rgba(0, 0, 0, 0.15));
}

.message-user .message-bubble {
  background-color: var(--theme-accent, #2563EB);
  color: #ffffff;
  border-bottom-right-radius: 4px;
  box-shadow: 0 4px 14px rgba(37, 99, 235, 0.3);
}

.message-time {
  display: block;
  font-size: 10.5px;
  color: var(--theme-text-muted, #64748B);
  margin-top: 5px;
}

.message-user .message-time {
  text-align: right;
  color: rgba(255, 255, 255, 0.7);
}

/* Стилизация подсказок (Suggestions Chips) */
.suggestions-grid {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: 10px;
  align-items: flex-start;
}

.suggestion-chip {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 8px 14px;
  background-color: var(--theme-panel-bg, #1E293B);
  border: 1px solid var(--theme-border-light, #334155);
  border-radius: 9999px;
  color: var(--theme-text-primary, #ffffff);
  font-size: 12.5px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.1);
  text-align: left;
}

.suggestion-chip:hover {
  background-color: var(--theme-accent, #2563EB);
  color: #ffffff;
  border-color: var(--theme-accent, #2563EB);
  transform: translateX(4px);
}

.chip-sparkle {
  color: var(--theme-accent, #3B82F6);
  font-size: 10px;
  letter-spacing: -2px;
}

.suggestion-chip:hover .chip-sparkle {
  color: #ffffff;
}

.chip-icon {
  font-size: 14px;
}

.chip-text {
  line-height: 1.2;
}

/* Индикатор набора */
.typing-bubble {
  display: flex;
  align-items: center;
  gap: 5px;
  padding: 12px 18px;
  background-color: var(--theme-card-bg, #111827);
  border: 1px solid var(--theme-border, #1E293B);
  border-radius: 14px;
  border-bottom-left-radius: 4px;
  width: fit-content;
}

.typing-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background-color: var(--theme-accent, #3B82F6);
  animation: typingPulse 1.4s infinite ease-in-out both;
}

.typing-dot:nth-child(1) { animation-delay: -0.32s; }
.typing-dot:nth-child(2) { animation-delay: -0.16s; }

@keyframes typingPulse {
  0%, 80%, 100% {
    transform: scale(0.6);
    opacity: 0.3;
  }
  40% {
    transform: scale(1.1);
    opacity: 1;
  }
}

/* Стилизация Markdown */
:deep(.message-text p) {
  margin: 0 0 8px 0;
}
:deep(.message-text p:last-child) {
  margin: 0;
}
:deep(.message-text ul), :deep(.message-text ol) {
  margin: 6px 0;
  padding-left: 20px;
}
:deep(.message-text li) {
  margin-bottom: 4px;
}
:deep(.message-text strong) {
  font-weight: 700;
  color: inherit;
}
:deep(.message-text table) {
  width: 100%;
  border-collapse: collapse;
  margin: 10px 0;
  font-size: 12px;
}
:deep(.message-text th), :deep(.message-text td) {
  border: 1px solid var(--theme-border-light, #334155);
  padding: 6px 10px;
  text-align: left;
}
:deep(.message-text th) {
  background-color: var(--theme-panel-bg, #1E293B);
  font-weight: 600;
}
:deep(.message-text code) {
  background-color: var(--theme-textbox-bg, #1E293B);
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 12px;
  font-family: monospace;
}
:deep(.message-text pre) {
  background-color: var(--theme-textbox-bg, #1E293B);
  padding: 8px 12px;
  border-radius: 8px;
  overflow-x: auto;
  margin: 8px 0;
}

/* Панель ввода */
.input-form {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 16px;
  background-color: var(--theme-card-bg, #111827);
  border-top: 1px solid var(--theme-border, #1E293B);
}

.chat-input {
  flex: 1;
  background-color: var(--theme-textbox-bg, #1E293B);
  border: 1px solid var(--theme-border, #1E293B);
  color: var(--theme-text-primary, #ffffff);
  border-radius: 12px;
  padding: 10px 14px;
  font-size: 13.5px;
  outline: none;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.chat-input:focus {
  border-color: var(--theme-accent, #2563EB);
  box-shadow: 0 0 0 2px rgba(37, 99, 235, 0.2);
}

.chat-input::placeholder {
  color: var(--theme-text-muted, #64748B);
}

.send-btn {
  width: 40px;
  height: 40px;
  border-radius: 10px;
  background-color: var(--theme-accent, #2563EB);
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  flex-shrink: 0;
  transition: all 0.2s ease;
}

.send-btn:hover:not(:disabled) {
  background-color: var(--theme-accent-hover, #1D4ED8);
  transform: translateY(-1px);
}

.send-btn:disabled {
  background-color: var(--theme-panel-bg, #1E293B);
  color: var(--theme-text-muted, #64748B);
  cursor: not-allowed;
  opacity: 0.6;
}

.send-icon {
  width: 18px;
  height: 18px;
}

/* Мобильная адаптивность */
@media (max-width: 480px) {
  .chat-window {
    width: calc(100vw - 32px);
    right: 16px;
    height: 520px;
  }
  .chatbot-container {
    bottom: 16px;
    right: 16px;
  }
}
</style>
