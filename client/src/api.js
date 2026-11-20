const BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:8090/api'

async function parseError(res, fallback) {
  let msg = fallback
  try {
    const data = await res.json()
    if (data && data.detail) {
      msg = typeof data.detail === 'string' ? data.detail : JSON.stringify(data.detail)
    }
  } catch (e) {
    try {
      const txt = await res.text()
      if (txt) msg = txt
    } catch (_) {}
  }
  return msg
}

export const api = {
  async getProducts(params = {}) {
    const query = new URLSearchParams()
    if (params.categoryId) query.append('categoryId', params.categoryId)
    if (params.brand) query.append('brand', params.brand)
    if (params.search) query.append('search', params.search)
    if (params.minPrice !== undefined && params.minPrice !== null && params.minPrice !== '') {
      query.append('minPrice', params.minPrice)
    }
    if (params.maxPrice !== undefined && params.maxPrice !== null && params.maxPrice !== '') {
      query.append('maxPrice', params.maxPrice)
    }
    if (params.inStock) query.append('inStock', 'true')

    const qs = query.toString()
    const res = await fetch(`${BASE_URL}/products${qs ? '?' + qs : ''}`, { cache: 'no-store' })
    if (!res.ok) throw new Error(await parseError(res, 'Не удалось загрузить товары'))
    return await res.json()
  },

  async getProduct(articul) {
    const res = await fetch(`${BASE_URL}/products/${articul}`, { cache: 'no-store' })
    if (!res.ok) throw new Error(await parseError(res, 'Товар не найден'))
    return await res.json()
  },

  async getCategories() {
    const res = await fetch(`${BASE_URL}/categories`, { cache: 'no-store' })
    if (!res.ok) throw new Error(await parseError(res, 'Не удалось загрузить категории'))
    return await res.json()
  },

  async getBrands() {
    const res = await fetch(`${BASE_URL}/brands`, { cache: 'no-store' })
    if (!res.ok) throw new Error(await parseError(res, 'Не удалось загрузить бренды'))
    return await res.json()
  },

  async getStatuses() {
    const res = await fetch(`${BASE_URL}/statuses`, { cache: 'no-store' })
    if (!res.ok) throw new Error(await parseError(res, 'Не удалось загрузить статусы'))
    return await res.json()
  },

  async login(email, password) {
    const res = await fetch(`${BASE_URL}/users/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    })
    if (!res.ok) {
      throw new Error(await parseError(res, 'Неверный email/телефон или пароль'))
    }
    return await res.json()
  },

  async register(name, email, phone, password) {
    const res = await fetch(`${BASE_URL}/users/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name, email, phone, password })
    })
    if (!res.ok) {
      throw new Error(await parseError(res, 'Ошибка регистрации'))
    }
    return await res.json()
  },

  async getUser(userId) {
    const res = await fetch(`${BASE_URL}/users/${userId}`, { cache: 'no-store' })
    if (!res.ok) throw new Error(await parseError(res, 'Пользователь не найден'))
    return await res.json()
  },

  async updateUser(userId, payload) {
    const res = await fetch(`${BASE_URL}/users/${userId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    })
    if (!res.ok) {
      throw new Error(await parseError(res, 'Не удалось обновить профиль'))
    }
    return await res.json()
  },

  async getCart(userId) {
    const res = await fetch(`${BASE_URL}/cart/${userId}`, { cache: 'no-store' })
    if (!res.ok) throw new Error(await parseError(res, 'Не удалось загрузить корзину'))
    return await res.json()
  },

  async addToCart(receiptId, productId, quantity = 1, variationId = null) {
    const res = await fetch(`${BASE_URL}/cart/add`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        receipt_id: receiptId,
        product_id: productId,
        quantity,
        variation_id: variationId
      })
    })
    if (!res.ok) throw new Error(await parseError(res, 'Не удалось добавить товар в корзину'))
    return await res.json()
  },

  async checkout(receiptId, address) {
    const res = await fetch(`${BASE_URL}/cart/checkout/${receiptId}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ address })
    })
    if (!res.ok) throw new Error(await parseError(res, 'Ошибка оформления заказа'))
    return await res.json()
  },

  async createOrder(orderData) {
    const res = await fetch(`${BASE_URL}/orders`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(orderData)
    })
    if (!res.ok) {
      throw new Error(await parseError(res, 'Ошибка создания заказа'))
    }
    return await res.json()
  },

  async getOrder(receiptId) {
    const res = await fetch(`${BASE_URL}/orders/${receiptId}`, { cache: 'no-store' })
    if (!res.ok) throw new Error(await parseError(res, 'Заказ не найден'))
    return await res.json()
  },

  async getOrders(userId) {
    const res = await fetch(`${BASE_URL}/orders/user/${userId}`, { cache: 'no-store' })
    if (!res.ok) throw new Error(await parseError(res, 'Не удалось загрузить историю заказов'))
    return await res.json()
  },

  async getReviews(articul) {
    const res = await fetch(`${BASE_URL}/products/${articul}/reviews`, { cache: 'no-store' })
    if (!res.ok) throw new Error(await parseError(res, 'Не удалось загрузить отзывы'))
    return await res.json()
  },

  async addReview(articul, userName, rating, comment, userId = null) {
    const res = await fetch(`${BASE_URL}/products/${articul}/reviews`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        user_id: userId,
        user_name: userName,
        rating,
        comment
      })
    })
    if (!res.ok) {
      throw new Error(await parseError(res, 'Не удалось добавить отзыв'))
    }
    return await res.json()
  },

  async sendChatMessage(messages) {
    const res = await fetch(`${BASE_URL}/chat`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ messages })
    })
    if (!res.ok) {
      throw new Error(await parseError(res, 'Не удалось получить ответ'))
    }
    return await res.json()
  }
}
