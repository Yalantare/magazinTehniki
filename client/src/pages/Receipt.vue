<template>
  <div class="container">
    <Header />

    <div class="receipt-wrapper">
      <div v-if="!receipt" class="not-found">
        <h2>Квитанция не найдена</h2>
        <button class="catalog-btn" @click="router.push('/catalog')">
          Вернуться в каталог
        </button>
      </div>

      <template v-else>
        <div class="success-icon-box">
          <Check :size="32" :stroke-width="3" />
        </div>

        <h1 class="page-title">Заказ оформлен!</h1>
        <p class="page-subtitle">Ваш заказ создан и обрабатывается</p>

        <div class="receipt-card">
          <div class="receipt-header">
            <div class="header-top-row">
              <div class="brand-group">
                <span style="font-size: 24px;">⚡</span>
                <span class="brand-title">МагазинТехники</span>
              </div>

              <div class="receipt-id-block">
                <div class="receipt-id-label">Идентификатор</div>
                <div class="receipt-id-value">{{ receipt.code }}</div>
              </div>
            </div>

            <div class="meta-grid">
              <div class="meta-col">
                <span class="meta-label">ДАТА ЗАКАЗА</span>
                <span class="meta-value">{{ formattedDate }}</span>
              </div>

              <div class="meta-col">
                <span class="meta-label">СТАТУС</span>
                <div class="status-badge">
                  <Clock :size="14" />
                  <span>{{ receipt.statusTitle }}</span>
                </div>
              </div>
            </div>
          </div>

          <div class="receipt-body">
            <div class="address-section">
              <div class="address-col">
                <span class="section-label">ПОЛУЧАТЕЛЬ</span>
                <span class="address-text">
                  {{ receipt.user?.name || store.user?.name || 'Покупатель' }}
                </span>
              </div>

              <div class="address-col">
                <span class="section-label">АДРЕС ДОСТАВКИ</span>
                <span class="address-text">{{ receipt.address || 'Самовывоз' }}</span>
              </div>
            </div>

            <div class="items-table-wrapper">
              <table class="items-table">
                <thead>
                  <tr>
                    <th>НАИМЕНОВАНИЕ</th>
                    <th style="text-align: center;">КОЛ-ВО</th>
                    <th style="text-align: right;">СУММА</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="item in receipt.receiptItems" :key="item.id">
                    <td>
                      <div class="item-name-cell">
                        <span class="item-title">{{ item.product?.title || 'Товар' }}</span>
                      </div>
                    </td>
                    <td style="text-align: center;" class="qty-cell">
                      {{ item.quantity }} шт.
                    </td>
                    <td style="text-align: right;" class="total-cell">
                      {{ (item.priceAtPurchase * item.quantity).toLocaleString('ru-RU') }} ₽
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <div class="totals-section">
              <div class="total-row">
                <span class="total-label">ИТОГО К ОПЛАТЕ</span>
                <span class="total-value">{{ receipt.totalPrice.toLocaleString('ru-RU') }} ₽</span>
              </div>
            </div>

            <div class="receipt-footer">
              Спасибо за покупку в магазине техники!
            </div>
          </div>
        </div>

        <div class="actions-row no-print">
          <button class="print-btn" @click="printReceipt">
            <Printer :size="18" />
            Распечатать чек
          </button>

          <button class="history-btn" @click="router.push('/profile?tab=history')">
            <ShoppingBag :size="18" />
            Мои заказы
          </button>

          <button class="catalog-link-btn" @click="router.push('/catalog')">
            В каталог товаров
          </button>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Check, Printer, Clock, ShoppingBag } from 'lucide-vue-next'
import Header from '../components/Header.vue'
import { store } from '../store.js'

const route = useRoute()
const router = useRouter()

const receipt = computed(() => {
  return store.orders.find(o => String(o.receiptId) === String(route.params.id)) || store.orders[0]
})

const formattedDate = computed(() => {
  if (!receipt.value) return ''
  const d = new Date(receipt.value.dateTime)
  return d.toLocaleDateString('ru-RU') + ' ' + d.toLocaleTimeString('ru-RU')
})

function printReceipt() {
  window.print()
}
</script>

<style scoped>
.container {
  min-height: 100vh;
  background-color: var(--theme-bg);
  display: flex;
  flex-direction: column;
}

.receipt-wrapper {
  max-width: 860px;
  margin: 0 auto;
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 40px 20px 80px 20px;
}

.not-found {
  padding: 60px;
  text-align: center;
}

.catalog-btn {
  margin-top: 20px;
  padding: 10px 24px;
  background-color: var(--theme-accent);
  color: #fff;
  border-radius: 8px;
  font-weight: 600;
}

.success-icon-box {
  width: 60px;
  height: 60px;
  border-radius: 12px;
  background-color: #064E3B;
  border: 2px solid rgba(16, 185, 129, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #10B981;
  margin-bottom: 20px;
}

.page-title {
  font-size: 36px;
  font-weight: 700;
  color: var(--theme-text-primary);
  text-align: center;
  margin-bottom: 8px;
}

.page-subtitle {
  font-size: 18px;
  color: var(--theme-text-secondary);
  text-align: center;
  margin-bottom: 35px;
}

.receipt-card {
  width: 100%;
  background-color: var(--theme-card-bg);
  border: 1px solid var(--theme-border);
  border-radius: 16px;
  overflow: hidden;
  box-shadow: var(--theme-shadow);
  margin-bottom: 30px;
}

.receipt-header {
  background: linear-gradient(135deg, #1E3A8A 0%, #2563EB 100%);
  padding: 30px 40px;
  color: #ffffff;
}

.header-top-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 25px;
}

.brand-group {
  display: flex;
  align-items: center;
  gap: 10px;
}

.brand-title {
  font-size: 22px;
  font-weight: 700;
  letter-spacing: -0.5px;
}

.receipt-id-block {
  text-align: right;
}

.receipt-id-label {
  font-size: 11px;
  text-transform: uppercase;
  letter-spacing: 1px;
  opacity: 0.8;
  margin-bottom: 2px;
}

.receipt-id-value {
  font-size: 18px;
  font-weight: 700;
  letter-spacing: 0.5px;
}

.meta-grid {
  display: grid;
  grid-template-columns: 1.2fr 1fr;
  gap: 20px;
  border-top: 1px solid rgba(255, 255, 255, 0.2);
  padding-top: 20px;
}

.meta-col {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.meta-label {
  font-size: 11px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  opacity: 0.8;
}

.meta-value {
  font-size: 15px;
  font-weight: 600;
}

.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background-color: rgba(16, 185, 129, 0.25);
  border: 1px solid #10B981;
  color: #ffffff;
  padding: 3px 8px;
  border-radius: 4px;
  font-size: 13px;
  font-weight: 600;
  width: fit-content;
}

.receipt-body {
  padding: 40px;
}

.address-section {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 30px;
  margin-bottom: 30px;
  padding-bottom: 25px;
  border-bottom: 1px solid var(--theme-border);
}

.address-col {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.section-label {
  font-size: 12px;
  font-weight: 700;
  color: var(--theme-text-muted);
  letter-spacing: 0.5px;
}

.address-text {
  font-size: 14px;
  color: var(--theme-text-primary);
  line-height: 1.5;
}

.items-table-wrapper {
  margin-bottom: 30px;
}

.items-table {
  width: 100%;
  border-collapse: collapse;
}

.items-table th {
  font-size: 12px;
  font-weight: 700;
  color: var(--theme-text-muted);
  letter-spacing: 0.5px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--theme-border);
  text-align: left;
}

.items-table td {
  padding: 16px 0;
  border-bottom: 1px solid var(--theme-border);
  color: var(--theme-text-primary);
}

.item-name-cell {
  display: flex;
  flex-direction: column;
}

.item-title {
  font-size: 15px;
  font-weight: 600;
}

.qty-cell {
  font-size: 14px;
  color: var(--theme-text-secondary);
}

.total-cell {
  font-size: 15px;
  font-weight: 700;
}

.totals-section {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 30px;
}

.total-row {
  display: flex;
  align-items: baseline;
  gap: 20px;
}

.total-label {
  font-size: 15px;
  font-weight: 700;
  color: var(--theme-text-secondary);
  letter-spacing: 0.5px;
}

.total-value {
  font-size: 28px;
  font-weight: 800;
  color: var(--theme-text-primary);
}

.receipt-footer {
  text-align: center;
  font-size: 13px;
  color: var(--theme-text-muted);
  line-height: 1.5;
  padding-top: 20px;
  border-top: 1px dashed var(--theme-border);
}

.actions-row {
  display: flex;
  gap: 16px;
  flex-wrap: wrap;
  justify-content: center;
}

.print-btn {
  background-color: var(--theme-accent);
  color: #ffffff;
  padding: 12px 24px;
  border-radius: 8px;
  font-size: 15px;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  transition: background-color 0.15s ease;
}

.print-btn:hover {
  background-color: var(--theme-accent-hover);
}

.history-btn {
  background-color: var(--theme-card-bg);
  border: 1px solid var(--theme-border);
  color: var(--theme-text-primary);
  padding: 12px 24px;
  border-radius: 8px;
  font-size: 15px;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  transition: all 0.15s ease;
}

.history-btn:hover {
  background-color: var(--theme-panel-bg);
}

.catalog-link-btn {
  background: transparent;
  color: var(--theme-accent);
  padding: 12px 20px;
  font-size: 15px;
  font-weight: 600;
  cursor: pointer;
}

.catalog-link-btn:hover {
  text-decoration: underline;
}

@media (max-width: 700px) {
  .meta-grid {
    grid-template-columns: 1fr;
    gap: 12px;
  }
  .address-section {
    grid-template-columns: 1fr;
    gap: 16px;
  }
  .receipt-header, .receipt-body {
    padding: 24px 20px;
  }
}
</style>
