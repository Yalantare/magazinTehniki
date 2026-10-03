<template>
  <div class="chatbot-container">
    <button
      v-if="!isOpen"
      class="chat-toggle-btn"
      @click="toggleChat"
      title="Онлайн-консультант"
    >
      <MessageSquare :size="24" />
    </button>

    <div v-if="isOpen" class="chat-window">
      <div class="chat-header">
        <div class="header-info">
          <div class="bot-avatar-box">
            <Bot :size="20" />
          </div>
          <div class="header-text">
            <h3 class="header-title">Онлайн-консультант</h3>
            <div class="status-row">
              <span class="status-indicator" />
              <span class="status-text">Онлайн | ТехноМир</span>
            </div>
          </div>
        </div>

        <button class="close-btn" @click="toggleChat" title="Закрыть">
          <X :size="20" />
        </button>
      </div>

      <div class="messages-container" ref="messagesEndRef">
        <div
          v-for="msg in messages"
          :key="msg.id"
          :class="['message-row', msg.sender === 'user' ? 'message-user' : 'message-bot']"
        >
          <div class="message-bubble">
            <p class="message-text">{{ msg.text }}</p>
            <span class="message-time">{{ msg.time }}</span>
          </div>
        </div>

        <div v-if="isTyping" class="message-row message-bot">
          <div class="typing-indicator-box">
            <span class="typing-square" />
            <span class="typing-square" />
            <span class="typing-square" />
          </div>
        </div>
      </div>

      <div class="suggestions-bar">
        <button
          v-for="sug in suggestions"
          :key="sug"
          class="suggestion-btn"
          @click="sendMessage(sug)"
        >
          {{ sug }}
        </button>
      </div>

      <form class="input-form" @submit.prevent="handleInputSubmit">
        <input
          ref="inputRef"
          v-model="inputValue"
          type="text"
          maxlength="200"
          placeholder="Напишите вопрос..."
          class="chat-input"
          @keydown="handleSpaceKeydown($event, inputValue, 30)"
          @input="inputValue = inputValue.replace(/^\s+/, '').replace(/\s{3,}/g, '  ').slice(0, 200)"
        />
        <button type="submit" class="send-btn" :disabled="!inputValue.trim()">
          <Send :size="18" />
        </button>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref, nextTick } from 'vue'
import { MessageSquare, X, Send, Bot } from 'lucide-vue-next'
import { handleSpaceKeydown } from '../utils/validators.js'

const isOpen = ref(false)
const inputValue = ref('')
const isTyping = ref(false)
const messagesEndRef = ref(null)
const inputRef = ref(null)

const suggestions = [
  'Какой смартфон выбрать?',
  'Условия доставки',
  'Гарантия и возврат',
  'Лучшие наушники',
  'Как оплатить заказ?'
]

const messages = ref([
  {
    id: 'msg-welcome-1',
    sender: 'bot',
    text: 'Здравствуйте! Я онлайн-консультант магазина техники. Чем могу помочь вам сегодня?',
    time: 'Сейчас'
  }
])

const getCurrentTime = () => {
  const now = new Date()
  return now.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })
}

const scrollToBottom = () => {
  nextTick(() => {
    if (messagesEndRef.value) {
      messagesEndRef.value.scrollTop = messagesEndRef.value.scrollHeight
    }
  })
}

const toggleChat = () => {
  isOpen.value = !isOpen.value
  if (isOpen.value) {
    scrollToBottom()
    setTimeout(() => {
      inputRef.value?.focus()
    }, 200)
  }
}

const getBotResponse = (query) => {
  const lower = query.toLowerCase()

  if (lower.includes('смартфон') || lower.includes('телефон') || lower.includes('айфон') || lower.includes('iphone')) {
    return 'В нашем каталоге представлены топовые модели: Apple iPhone 15 Pro для любителей экосистемы iOS и Samsung Galaxy S24 Ultra с потрясающим дисплеем Dynamic AMOLED 2X. Вы можете сравнить их характеристики прямо в каталоге!'
  }

  if (lower.includes('доставк') || lower.includes('курьер') || lower.includes('привез')) {
    return 'У нас действует бесплатная доставка при заказе от 50 000 ₽! Срок доставки курьером до двери - 1-2 рабочих дня. Также доступен бесплатный самовывоз из нашего пункта выдачи.'
  }

  if (lower.includes('гаранти') || lower.includes('возврат') || lower.includes('брак') || lower.includes('ремонт')) {
    return 'На все оригинальные товары предоставляется официальная гарантия 12 месяцев. В течение 14 дней возможен возврат или обмен товара надлежащего качества.'
  }

  if (lower.includes('наушник') || lower.includes('звук') || lower.includes('аудио') || lower.includes('airpods') || lower.includes('sony')) {
    return 'Для лучшего шумоподавления и музыки рекомендуем Sony WH-1000XM5. Если вам нужны компактные TWS-наушники - обратите внимание на Apple AirPods Pro 3!'
  }

  if (lower.includes('оплат') || lower.includes('карт') || lower.includes('сбп') || lower.includes('рассрочк')) {
    return 'Вы можете оплатить заказ банковской картой (МИР, Visa, Mastercard), через СБП с QR-кодом, либо наличными курьеру при получении заказа.'
  }

  if (lower.includes('наличи') || lower.includes('склад')) {
    return 'Все товары со статусом "В наличии" находятся на складе и готовы к быстрой отправке в день оформления!'
  }

  return 'Спасибо за вопрос! Вы можете добавить любой понравившийся товар в корзину и оформить заказ в пару кликов. Если потребуется детальная консультация, наши специалисты свяжутся с вами при подтверждении заказа.'
}

const sendMessage = (textToSend) => {
  const text = (textToSend || inputValue.value).trim()
  if (!text) return

  messages.value.push({
    id: `user-${Date.now()}`,
    sender: 'user',
    text,
    time: getCurrentTime()
  })

  inputValue.value = ''
  isTyping.value = true
  scrollToBottom()

  setTimeout(() => {
    const reply = getBotResponse(text)
    messages.value.push({
      id: `bot-${Date.now()}`,
      sender: 'bot',
      text: reply,
      time: getCurrentTime()
    })
    isTyping.value = false
    scrollToBottom()
  }, 700)
}

const handleInputSubmit = () => {
  sendMessage()
}
</script>

<style scoped>
.chatbot-container {
  position: fixed;
  bottom: 30px;
  right: 30px;
  z-index: 1000;
}

.chat-toggle-btn {
  width: 56px;
  height: 56px;
  border-radius: 12px;
  background-color: var(--theme-accent);
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 8px 24px rgba(37, 99, 235, 0.4);
  cursor: pointer;
  transition: transform 0.2s ease, background-color 0.2s ease;
}

.chat-toggle-btn:hover {
  transform: translateY(-2px);
  background-color: var(--theme-accent-hover);
}

.chat-window {
  width: 380px;
  height: 520px;
  background-color: var(--theme-card-bg);
  border: 1px solid var(--theme-border);
  border-radius: 16px;
  display: flex;
  flex-direction: column;
  box-shadow: 0 16px 40px rgba(0, 0, 0, 0.35);
  overflow: hidden;
}

.chat-header {
  background: linear-gradient(135deg, #1E3A8A 0%, #2563EB 100%);
  padding: 16px 20px;
  color: #ffffff;
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
  width: 36px;
  height: 36px;
  border-radius: 8px;
  background-color: rgba(255, 255, 255, 0.2);
  display: flex;
  align-items: center;
  justify-content: center;
}

.header-text {
  display: flex;
  flex-direction: column;
}

.header-title {
  font-size: 15px;
  font-weight: 700;
  line-height: 1.2;
}

.status-row {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-top: 2px;
}

.status-indicator {
  width: 7px;
  height: 7px;
  border-radius: 2px;
  background-color: #10B981;
}

.status-text {
  font-size: 11px;
  opacity: 0.85;
}

.close-btn {
  background: transparent;
  color: #ffffff;
  width: 32px;
  height: 32px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: background-color 0.15s;
}

.close-btn:hover {
  background-color: rgba(255, 255, 255, 0.15);
}

.messages-container {
  flex: 1;
  padding: 16px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 12px;
  background-color: var(--theme-bg);
}

.message-row {
  display: flex;
  flex-direction: column;
  max-width: 82%;
}

.message-bot {
  align-self: flex-start;
}

.message-user {
  align-self: flex-end;
}

.message-bubble {
  padding: 12px 14px;
  border-radius: 12px;
  font-size: 13px;
  line-height: 1.5;
}

.message-bot .message-bubble {
  background-color: var(--theme-card-bg);
  border: 1px solid var(--theme-border);
  color: var(--theme-text-primary);
  border-bottom-left-radius: 4px;
}

.message-user .message-bubble {
  background-color: var(--theme-accent);
  color: #ffffff;
  border-bottom-right-radius: 4px;
}

.message-text {
  word-break: break-word;
}

.message-time {
  display: block;
  font-size: 10px;
  margin-top: 4px;
  text-align: right;
  opacity: 0.7;
}

.typing-indicator-box {
  display: flex;
  align-items: center;
  gap: 5px;
  padding: 12px 16px;
  background-color: var(--theme-card-bg);
  border: 1px solid var(--theme-border);
  border-radius: 12px;
  border-bottom-left-radius: 4px;
  width: fit-content;
}

.typing-square {
  width: 6px;
  height: 6px;
  border-radius: 2px;
  background-color: var(--theme-text-muted);
  animation: typingBounce 1.4s infinite ease-in-out both;
}

.typing-square:nth-child(1) {
  animation-delay: -0.32s;
}

.typing-square:nth-child(2) {
  animation-delay: -0.16s;
}

@keyframes typingBounce {
  0%, 80%, 100% {
    transform: scale(0.6);
    opacity: 0.4;
  }
  40% {
    transform: scale(1);
    opacity: 1;
  }
}

.suggestions-bar {
  display: flex;
  gap: 6px;
  padding: 8px 12px;
  overflow-x: auto;
  background-color: var(--theme-card-bg);
  border-top: 1px solid var(--theme-border);
  white-space: nowrap;
}

.suggestions-bar::-webkit-scrollbar {
  height: 4px;
}

.suggestion-btn {
  background-color: var(--theme-panel-bg);
  border: 1px solid var(--theme-border);
  color: var(--theme-text-secondary);
  font-size: 11px;
  font-weight: 500;
  padding: 5px 10px;
  border-radius: 6px;
  cursor: pointer;
  flex-shrink: 0;
  transition: all 0.15s;
}

.suggestion-btn:hover {
  color: var(--theme-text-primary);
  border-color: var(--theme-accent);
}

.input-form {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 16px;
  background-color: var(--theme-card-bg);
  border-top: 1px solid var(--theme-border);
}

.chat-input {
  flex: 1;
  background-color: var(--theme-textbox-bg);
  border: 1px solid var(--theme-border);
  color: var(--theme-text-primary);
  border-radius: 8px;
  padding: 10px 12px;
  font-size: 13px;
  outline: none;
  transition: border-color 0.15s;
}

.chat-input:focus {
  border-color: var(--theme-accent);
}

.send-btn {
  width: 38px;
  height: 38px;
  border-radius: 8px;
  background-color: var(--theme-accent);
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: background-color 0.15s;
}

.send-btn:hover:not(:disabled) {
  background-color: var(--theme-accent-hover);
}

.send-btn:disabled {
  background-color: var(--theme-textbox-bg);
  color: var(--theme-text-muted);
  cursor: not-allowed;
}

@media (max-width: 480px) {
  .chat-window {
    width: calc(100vw - 32px);
    height: 480px;
    right: 16px;
    bottom: 16px;
  }
  .chatbot-container {
    right: 16px;
    bottom: 16px;
  }
}
</style>
