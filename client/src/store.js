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
  notifications: [],
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
      if (Array.isArray(prods)) {
        const prodBrands = prods.map(p => p.manufacturer).filter(Boolean)
        const combined = Array.from(new Set([...(this.brands || []), ...prodBrands])).sort((a, b) => a.localeCompare(b))
        this.brands = combined
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

  notify(content, type = 'info', duration = 4000) {
    const id = Date.now() + Math.random()
    let toast = { id, type, duration }
    if (typeof content === 'string') {
      toast.message = content
    } else if (typeof content === 'object' && content !== null) {
      toast = { ...toast, ...content, id }
    }
    this.notifications.push(toast)
    if (duration > 0) {
      setTimeout(() => {
        this.removeNotification(id)
      }, duration)
    }
    return id
  },

  removeNotification(id) {
    this.notifications = this.notifications.filter(n => n.id !== id)
  },

  getQuantityInCart(articul, variationId = null) {
    const key = variationId ? `${articul}-${variationId}` : `${articul}`
    const item = this.cart.find(it => it.id === key)
    return item ? item.quantity : 0
  },

  addToCart(product, variation = null, quantity = 1, silent = false) {
    if (!product) return { success: false, reason: 'no_product' }

    const key = variation ? `${product.articul}-${variation.id}` : `${product.articul}`
    const price = variation ? variation.price : product.price
    const stock = Number(variation ? (variation.stock ?? 0) : (product.stock ?? 0))
    const title = product.title + (variation ? ` (${variation.name})` : '')

    if (stock <= 0) {
      const message = `Товара «${title}» нет в наличии на складе.`
      if (!silent) {
        this.notify({
          title: 'Нет на складе',
          message,
          type: 'warning'
        })
      }
      return { success: false, reason: 'out_of_stock', stock: 0, inCart: 0, available: 0, message }
    }

    const existingIndex = this.cart.findIndex(item => item.id === key)
    const currentInCart = existingIndex >= 0 ? this.cart[existingIndex].quantity : 0

    if (currentInCart + quantity > stock) {
      const remaining = Math.max(0, stock - currentInCart)
      let message = ''
      if (currentInCart >= stock) {
        message = `Нельзя добавить больше товара, чем есть на складе! В наличии: ${stock} шт., и все они уже в вашей корзине.`
      } else {
        message = `Нельзя добавить столько товара, сколько нет на складе! Доступно еще: ${remaining} шт. (в корзине: ${currentInCart} шт., на складе: ${stock} шт.)`
      }
      if (!silent) {
        this.notify({
          title: 'Ограничение по складу',
          message,
          type: 'warning'
        })
      }
      return {
        success: false,
        reason: 'stock_exceeded',
        stock,
        inCart: currentInCart,
        available: remaining,
        message
      }
    }

    if (existingIndex >= 0) {
      this.cart[existingIndex].quantity += quantity
      this.cart[existingIndex].displayStock = stock
      this.cart[existingIndex].displayPrice = price
    } else {
      this.cart.push({
        id: key,
        product,
        productVariation: variation,
        quantity,
        displayPrice: price,
        displayStock: stock
      })
    }

    localStorage.setItem('cart', JSON.stringify(this.cart))

    if (!silent) {
      this.notify({
        title: 'Корзина',
        message: `Товар «${title}» добавлен в корзину (${quantity} шт.)`,
        type: 'success',
        duration: 2500
      })
    }

    return {
      success: true,
      addedQuantity: quantity,
      inCart: currentInCart + quantity,
      stock,
      message: 'Товар добавлен в корзину'
    }
  },

  removeFromCart(id) {
    this.cart = this.cart.filter(item => item.id !== id)
    localStorage.setItem('cart', JSON.stringify(this.cart))
  },

  updateQuantity(id, quantity) {
    if (quantity <= 0) {
      this.removeFromCart(id)
      return true
    }
    for (let i = 0; i < this.cart.length; i++) {
      if (this.cart[i].id === id) {
        const stock = this.cart[i].displayStock
        if (quantity > stock) {
          const message = `Нельзя добавить больше товара, чем есть на складе! В наличии: ${stock} шт.`
          this.notify({
            title: 'Ограничение по складу',
            message,
            type: 'warning'
          })
          return false
        }
        this.cart[i].quantity = quantity
        localStorage.setItem('cart', JSON.stringify(this.cart))
        return true
      }
    }
    return false
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
