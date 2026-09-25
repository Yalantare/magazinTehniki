const BASE_URL = 'http://localhost:8090/api'

export const api = {
  async getProducts(params = {}) {
    const query = new URLSearchParams()
    if (params.categoryId) query.append('categoryId', params.categoryId)
    if (params.brand) query.append('brand', params.brand)
    if (params.search) query.append('search', params.search)
    if (params.minPrice) query.append('minPrice', params.minPrice)
    if (params.maxPrice) query.append('maxPrice', params.maxPrice)
    if (params.inStock) query.append('inStock', 'true')

    const res = await fetch(`${BASE_URL}/products?${query.toString()}`)
    return await res.json()
  },

  async getProduct(articul) {
    const res = await fetch(`${BASE_URL}/products/${articul}`)
    return await res.json()
  },

  async getCategories() {
    const res = await fetch(`${BASE_URL}/categories`)
    return await res.json()
  },

  async getBrands() {
    const res = await fetch(`${BASE_URL}/brands`)
    return await res.json()
  },

  async login(email, password) {
    const res = await fetch(`${BASE_URL}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    })
    if (!res.ok) throw new Error('Ошибка входа')
    return await res.json()
  },

  async register(name, email, phone, password) {
    const res = await fetch(`${BASE_URL}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name, email, phone, password })
    })
    if (!res.ok) throw new Error('Ошибка регистрации')
    return await res.json()
  },

  async getCart(userId) {
    const res = await fetch(`${BASE_URL}/cart/${userId}`)
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
    return await res.json()
  },

  async checkout(receiptId, address) {
    const res = await fetch(`${BASE_URL}/cart/checkout/${receiptId}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ address })
    })
    return await res.json()
  },

  async createOrder(orderData) {
    const res = await fetch(`${BASE_URL}/orders`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(orderData)
    })
    if (!res.ok) {
      const err = await res.text()
      throw new Error(`Ошибка создания заказа: ${err}`)
    }
    return await res.json()
  },

  async getOrder(receiptId) {
    const res = await fetch(`${BASE_URL}/orders/${receiptId}`)
    if (!res.ok) throw new Error('Заказ не найден')
    return await res.json()
  },

  async getOrders(userId) {
    const res = await fetch(`${BASE_URL}/orders/user/${userId}`)
    return await res.json()
  },

  async getReviews(articul) {
    const res = await fetch(`${BASE_URL}/products/${articul}/reviews`)
    return await res.json()
  },

  async addReview(articul, userName, rating, comment) {
    const res = await fetch(`${BASE_URL}/products/${articul}/reviews`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        user_name: userName,
        rating,
        comment
      })
    })
    return await res.json()
  }
}
