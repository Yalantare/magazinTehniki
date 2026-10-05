import { reactive } from 'vue'
import { products, categories, brands, orders, reviews, user } from './data.js'
import { api } from './api.js'

export const store = reactive({
  user: JSON.parse(localStorage.getItem('user')) || null,
  isGuest: localStorage.getItem('isGuest') === 'true' || !localStorage.getItem('user'),
  theme: localStorage.getItem('theme') || 'dark',
  cart: JSON.parse(localStorage.getItem('cart')) || [],
  orders: JSON.parse(localStorage.getItem('orders')) || orders,
  reviews: JSON.parse(localStorage.getItem('reviews')) || reviews,
  products,
  categories,
  brands,

  async loadFromApi() {
    try {
      const prods = await api.getProducts()
      if (Array.isArray(prods) && prods.length > 0) {
        this.products = prods
      }
      const cats = await api.getCategories()
      if (Array.isArray(cats) && cats.length > 0) {
        this.categories = cats
      }
      const brs = await api.getBrands()
      if (Array.isArray(brs) && brs.length > 0) {
        this.brands = brs
      }
      if (this.user && this.user.userId) {
        await this.loadOrders()
      }
    } catch (e) {
    }
  },

  async loadOrders() {
    try {
      if (this.user && this.user.userId) {
        const dbOrders = await api.getOrders(this.user.userId)
        if (Array.isArray(dbOrders) && dbOrders.length > 0) {
          const ids = new Set(dbOrders.map(o => String(o.receiptId)))
          const localOnly = this.orders.filter(o => !ids.has(String(o.receiptId)))
          this.orders = [...dbOrders, ...localOnly]
          localStorage.setItem('orders', JSON.stringify(this.orders))
        }
      }
    } catch (e) {
      console.warn('Could not load orders from API:', e)
    }
  },

  initTheme() {
    document.documentElement.setAttribute('data-theme', this.theme)
  },

  toggleTheme() {
    this.theme = this.theme === 'dark' ? 'light' : 'dark'
    document.documentElement.setAttribute('data-theme', this.theme)
    localStorage.setItem('theme', this.theme)
  },

  async login(email, pass) {
    try {
      const u = await api.login(email, pass)
      this.user = {
        userId: u.userId,
        role: u.role,
        name: u.name,
        phone: u.phone,
        email: u.email
      }
      this.isGuest = false
      localStorage.setItem('user', JSON.stringify(this.user))
      localStorage.setItem('isGuest', 'false')
      await this.loadOrders()
      return true
    } catch (e) {
      console.warn('Login error:', e)
      throw e
    }
  },

  loginGuest() {
    this.user = null
    this.isGuest = true
    localStorage.removeItem('user')
    localStorage.setItem('isGuest', 'true')
  },

  async register(name, email, phone, pass) {
    try {
      const u = await api.register(name, email, phone, pass)
      this.user = {
        userId: u.userId,
        role: u.role,
        name: u.name,
        phone: u.phone,
        email: u.email
      }
      this.isGuest = false
      localStorage.setItem('user', JSON.stringify(this.user))
      localStorage.setItem('isGuest', 'false')
      return true
    } catch (e) {
      this.user = {
        userId: Date.now(),
        role: 'user',
        name,
        email,
        phone,
        password: pass
      }
      this.isGuest = false
      localStorage.setItem('user', JSON.stringify(this.user))
      localStorage.setItem('isGuest', 'false')
      return false
    }
  },

  logout() {
    this.user = null
    this.isGuest = true
    localStorage.removeItem('user')
    localStorage.setItem('isGuest', 'true')
  },

  updateUser(data) {
    if (this.user) {
      this.user = { ...this.user, ...data }
      localStorage.setItem('user', JSON.stringify(this.user))
    }
  },

  getCartCount() {
    let count = 0
    for (let i = 0; i < this.cart.length; i++) {
      count += this.cart[i].quantity
    }
    return count
  },

  getCartSubtotal() {
    let sum = 0
    for (let i = 0; i < this.cart.length; i++) {
      sum += this.cart[i].displayPrice * this.cart[i].quantity
    }
    return sum
  },

  getDelivery() {
    if (this.cart.length === 0) return 0
    return this.getCartSubtotal() >= 50000 ? 0 : 500
  },

  getCartTotal() {
    return this.getCartSubtotal() + this.getDelivery()
  },

  addToCart(product, variation = null, quantity = 1) {
    const key = variation ? `${product.articul}-${variation.id}` : `${product.articul}`
    const price = variation ? variation.price : product.price
    const stock = variation ? variation.stock : product.stock

    for (let i = 0; i < this.cart.length; i++) {
      if (this.cart[i].id === key) {
        if (this.cart[i].quantity + quantity <= stock) {
          this.cart[i].quantity += quantity
        }
        localStorage.setItem('cart', JSON.stringify(this.cart))
        return
      }
    }

    this.cart.push({
      id: key,
      product,
      productVariation: variation,
      quantity: Math.min(stock, quantity),
      displayPrice: price,
      displayStock: stock
    })
    localStorage.setItem('cart', JSON.stringify(this.cart))
  },

  removeFromCart(id) {
    this.cart = this.cart.filter(item => item.id !== id)
    localStorage.setItem('cart', JSON.stringify(this.cart))
  },

  updateQuantity(id, quantity) {
    if (quantity <= 0) {
      this.removeFromCart(id)
      return
    }
    for (let i = 0; i < this.cart.length; i++) {
      if (this.cart[i].id === id) {
        this.cart[i].quantity = Math.min(this.cart[i].displayStock, quantity)
        break
      }
    }
    localStorage.setItem('cart', JSON.stringify(this.cart))
  },

  clearCart() {
    this.cart = []
    localStorage.setItem('cart', JSON.stringify(this.cart))
  },

  async createOrder(address, customerName, customerPhone) {
    const items = []
    for (let i = 0; i < this.cart.length; i++) {
      items.push({
        id: Date.now() + i,
        productId: this.cart[i].product.articul,
        quantity: this.cart[i].quantity,
        priceAtPurchase: this.cart[i].displayPrice,
        product: this.cart[i].product,
        productVariation: this.cart[i].productVariation
      })
    }

    const payload = {
      user_id: this.user ? this.user.userId : null,
      name: customerName || (this.user ? this.user.name : 'Покупатель'),
      phone: customerPhone || (this.user ? this.user.phone : ''),
      email: this.user ? this.user.email : null,
      address,
      items: items.map(it => ({
        product_id: it.productId,
        variation_id: it.productVariation ? it.productVariation.id : null,
        quantity: it.quantity,
        price: it.priceAtPurchase
      }))
    }

    let serverOrder = null
    try {
      serverOrder = await api.createOrder(payload)
    } catch (e) {
      console.error('API createOrder failed, fallback to local:', e)
    }

    const newOrder = serverOrder || {
      receiptId: Date.now(),
      code: '#TF-2026-000' + (this.orders.length + 1),
      userId: this.user ? this.user.userId : 2,
      totalPrice: this.getCartTotal(),
      dateTime: new Date().toISOString(),
      statusTitle: 'В обработке',
      address,
      user: this.user,
      receiptItems: items
    }

    if (!newOrder.receiptItems || newOrder.receiptItems.length === 0) {
      newOrder.receiptItems = items
    }

    this.orders.unshift(newOrder)
    localStorage.setItem('orders', JSON.stringify(this.orders))
    this.clearCart()
    return newOrder
  },

  getReviews(articul) {
    return this.reviews[articul] || []
  },

  async loadReviews(articul) {
    if (!articul) return
    try {
      const serverRevs = await api.getReviews(articul)
      if (Array.isArray(serverRevs)) {
        this.reviews[articul] = serverRevs
        localStorage.setItem('reviews', JSON.stringify(this.reviews))
      }
    } catch (e) {
    }
  },

  getRating(articul) {
    const revs = this.getReviews(articul)
    if (revs.length === 0) {
      const prod = this.products.find(p => p.articul === articul)
      return {
        rating: prod ? (prod.rating || 5) : 5,
        count: prod ? (prod.reviewsCount || 0) : 0
      }
    }
    let sum = 0
    for (let i = 0; i < revs.length; i++) {
      sum += revs[i].rating
    }
    const avg = sum / revs.length
    return {
      rating: Math.round(avg * 10) / 10,
      count: revs.length
    }
  },

  async addReview(articul, rating, comment, name) {
    const author = name || (this.user ? this.user.name : 'Покупатель')
    const now = new Date()
    const dateStr = now.toLocaleDateString('ru-RU')

    let serverRev = null
    try {
      serverRev = await api.addReview(articul, author, rating, comment, this.user ? this.user.userId : null)
    } catch (e) {
      console.warn('Could not post review to API, saving locally:', e)
    }

    if (!this.reviews[articul]) {
      this.reviews[articul] = []
    }

    const newRev = serverRev || {
      id: 'rev-' + Date.now(),
      articul,
      userName: author,
      rating,
      date: dateStr,
      comment
    }

    this.reviews[articul].unshift(newRev)
    localStorage.setItem('reviews', JSON.stringify(this.reviews))

    const prod = this.products.find(p => p.articul === articul)
    if (prod) {
      const revs = this.reviews[articul]
      const sum = revs.reduce((acc, r) => acc + r.rating, 0)
      prod.rating = Math.round((sum / revs.length) * 10) / 10
      prod.reviewsCount = revs.length
    }
  }
})
