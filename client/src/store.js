import { reactive } from 'vue'
import { api } from './api.js'

function safeParse(key, fallback) {
  try {
    const raw = localStorage.getItem(key)
    return raw ? JSON.parse(raw) : fallback
  } catch (e) {
    return fallback
  }
}

export const store = reactive({
  user: safeParse('user', null),
  isGuest: localStorage.getItem('isGuest') === 'true' || !localStorage.getItem('user'),
  theme: localStorage.getItem('theme') || 'dark',
  cart: safeParse('cart', []),
  orders: [],
  reviews: {},
  notifications: [],
  products: [],
  categories: [],
  brands: [],
  isLoading: false,
  isLoaded: false,
  apiError: null,

  getProductStock(product) {
    if (!product) return 0
    const vars = Array.isArray(product.productVariations) ? product.productVariations : []
    if (vars.length > 0) {
      return vars.reduce((sum, v) => sum + Math.max(0, Number(v.stock) || 0), 0)
    }
    return Math.max(0, Number(product.stock) || 0)
  },

  getProductDisplayPrice(product) {
    if (!product) return 0
    const vars = Array.isArray(product.productVariations) ? product.productVariations : []
    if (vars.length > 0) {
      const availableVars = vars.filter(v => (Number(v.stock) || 0) > 0)
      const source = availableVars.length > 0 ? availableVars : vars
      const prices = source.map(v => Number(v.price)).filter(p => Number.isFinite(p) && p > 0)
      if (prices.length > 0) {
        return Math.min(...prices)
      }
    }
    return Number(product.price) || 0
  },

  hasSelectableVariations(product) {
    const vars = Array.isArray(product?.productVariations) ? product.productVariations : []
    if (vars.length > 1) return true
    if (vars.length === 1) {
      const name = String(vars[0]?.name || '').trim().toLowerCase()
      return name !== '' && name !== 'базовая'
    }
    return false
  },

  syncCartWithProducts() {
    if (!Array.isArray(this.cart) || this.cart.length === 0) return
    if (!Array.isArray(this.products)) return

    let changed = false
    const updatedCart = []

    for (const item of this.cart) {
      const freshProduct = this.products.find(
        p => String(p.articul) === String(item.product?.articul)
      )
      if (!freshProduct) {
        changed = true
        continue
      }

      const freshVars = Array.isArray(freshProduct.productVariations)
        ? freshProduct.productVariations
        : []

      let freshVar = null
      if (item.productVariation?.id != null) {
        freshVar = freshVars.find(v => String(v.id) === String(item.productVariation.id)) || null
        if (!freshVar && freshVars.length > 0) {
          // Если старая вариация была удалена в БД, удаляем устаревшую позицию из корзины
          changed = true
          continue
        }
      } else if (freshVars.length === 1) {
        // Если в БД для товара появилась единственная вариация (например, «Базовая»), привязываемся к ней
        freshVar = freshVars[0]
      }

      const freshStock = freshVar
        ? Math.max(0, Number(freshVar.stock) || 0)
        : this.getProductStock(freshProduct)
      const freshPrice = freshVar
        ? (Number(freshVar.price) || Number(freshProduct.price) || 0)
        : (Number(freshProduct.price) || 0)

      if (freshStock <= 0) {
        changed = true
        continue
      }

      const nextQty = Math.min(Math.max(1, Number(item.quantity) || 1), freshStock)
      const nextId = freshVar ? `${freshProduct.articul}-${freshVar.id}` : `${freshProduct.articul}`

      if (
        nextId !== item.id ||
        nextQty !== item.quantity ||
        freshStock !== item.displayStock ||
        freshPrice !== item.displayPrice
      ) {
        changed = true
      }

      const existingIdx = updatedCart.findIndex(x => x.id === nextId)
      if (existingIdx >= 0) {
        updatedCart[existingIdx].quantity = Math.min(
          updatedCart[existingIdx].quantity + nextQty,
          freshStock
        )
        updatedCart[existingIdx].displayStock = freshStock
        updatedCart[existingIdx].displayPrice = freshPrice
        updatedCart[existingIdx].product = freshProduct
        updatedCart[existingIdx].productVariation = freshVar
        changed = true
      } else {
        updatedCart.push({
          ...item,
          id: nextId,
          product: freshProduct,
          productVariation: freshVar,
          quantity: nextQty,
          displayPrice: freshPrice,
          displayStock: freshStock
        })
      }
    }

    if (changed || updatedCart.length !== this.cart.length) {
      this.cart = updatedCart
      localStorage.setItem('cart', JSON.stringify(this.cart))
    } else {
      // Обновляем ссылки на свежие объекты товаров/вариаций
      this.cart = updatedCart
    }
  },

  async loadFromApi(silent = false) {
    if (!silent) this.isLoading = true
    this.apiError = null
    try {
      const [prods, cats, brs] = await Promise.all([
        api.getProducts(),
        api.getCategories(),
        api.getBrands()
      ])

      this.products = Array.isArray(prods) ? prods : []
      this.categories = Array.isArray(cats) ? cats : []

      const brandSet = new Set(Array.isArray(brs) ? brs.map(b => String(b).trim()).filter(Boolean) : [])
      for (const p of this.products) {
        if (p.manufacturer && String(p.manufacturer).trim()) {
          brandSet.add(String(p.manufacturer).trim())
        }
      }
      this.brands = Array.from(brandSet).sort((a, b) => a.localeCompare(b, 'ru'))

      this.syncCartWithProducts()
      this.isLoaded = true

      if (this.user && this.user.userId) {
        await this.loadOrders()
      }
    } catch (e) {
      console.error('Ошибка загрузки данных из БД:', e)
      this.apiError = e.message || 'Не удалось подключиться к серверу API'
    } finally {
      if (!silent) this.isLoading = false
    }
  },

  async loadProduct(articul) {
    if (!articul) return null
    try {
      const fresh = await api.getProduct(articul)
      if (!fresh || fresh.articul == null) return null

      const idx = this.products.findIndex(p => String(p.articul) === String(fresh.articul))
      if (idx >= 0) {
        this.products.splice(idx, 1, fresh)
      } else {
        this.products.push(fresh)
      }
      this.syncCartWithProducts()
      return fresh
    } catch (e) {
      console.error('Ошибка загрузки товара из БД:', e)
      return null
    }
  },

  async loadOrders() {
    try {
      if (this.user && this.user.userId) {
        const dbOrders = await api.getOrders(this.user.userId)
        this.orders = Array.isArray(dbOrders) ? dbOrders : []
      } else {
        this.orders = []
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
    this.orders = []
    localStorage.removeItem('user')
    localStorage.setItem('isGuest', 'true')
  },

  async register(name, email, phone, pass) {
    const u = await api.register(name, email, phone, pass)
    this.user = {
      userId: u.userId,
      role: u.role,
      name: u.name,
      phone: u.phone,
      email: u.email
    }
    this.isGuest = false
    this.orders = []
    localStorage.setItem('user', JSON.stringify(this.user))
    localStorage.setItem('isGuest', 'false')
    return true
  },

  logout() {
    this.user = null
    this.isGuest = true
    this.orders = []
    localStorage.removeItem('user')
    localStorage.setItem('isGuest', 'true')
  },

  async updateUser(data) {
    if (!this.user || !this.user.userId) return
    const payload = {
      name: data.name,
      phone: data.phone,
      email: data.email
    }
    if (data.password && String(data.password).trim().length > 0) {
      payload.password = data.password
    }
    const updated = await api.updateUser(this.user.userId, payload)
    this.user = {
      userId: updated.userId,
      role: updated.role,
      name: updated.name,
      phone: updated.phone,
      email: updated.email
    }
    localStorage.setItem('user', JSON.stringify(this.user))
    return this.user
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
    if (variationId !== null && variationId !== undefined) {
      const key = `${articul}-${variationId}`
      const item = this.cart.find(it => it.id === key)
      return item ? item.quantity : 0
    }
    return this.cart.reduce((sum, it) => {
      return String(it.product?.articul) === String(articul) ? sum + (Number(it.quantity) || 0) : sum
    }, 0)
  },

  addToCart(product, variation = null, quantity = 1, silent = false) {
    if (!product) return { success: false, reason: 'no_product' }

    // Всегда сверяемся со свежими данными товара из store.products
    const freshProduct =
      this.products.find(p => String(p.articul) === String(product.articul)) || product
    const freshVars = Array.isArray(freshProduct.productVariations)
      ? freshProduct.productVariations
      : []

    let targetVar = null
    if (variation && variation.id != null) {
      targetVar = freshVars.find(v => String(v.id) === String(variation.id)) || variation
    } else if (freshVars.length === 1) {
      targetVar = freshVars[0]
    }

    const key = targetVar ? `${freshProduct.articul}-${targetVar.id}` : `${freshProduct.articul}`
    const price = targetVar
      ? (Number(targetVar.price) || Number(freshProduct.price) || 0)
      : (Number(freshProduct.price) || 0)
    const stock = targetVar
      ? Math.max(0, Number(targetVar.stock) || 0)
      : this.getProductStock(freshProduct)

    const varLabel =
      targetVar && targetVar.name && targetVar.name.toLowerCase() !== 'базовая'
        ? ` (${targetVar.name})`
        : ''
    const title = freshProduct.title + varLabel

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
      this.cart[existingIndex].product = freshProduct
      this.cart[existingIndex].productVariation = targetVar
    } else {
      this.cart.push({
        id: key,
        product: freshProduct,
        productVariation: targetVar,
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

  getLiveCartItemStock(item) {
    if (!item) return 0
    const freshProduct = this.products.find(
      p => String(p.articul) === String(item.product?.articul)
    )
    if (!freshProduct) return Math.max(0, Number(item.displayStock) || 0)

    const freshVars = Array.isArray(freshProduct.productVariations)
      ? freshProduct.productVariations
      : []
    if (item.productVariation?.id != null) {
      const v = freshVars.find(x => String(x.id) === String(item.productVariation.id))
      return v ? Math.max(0, Number(v.stock) || 0) : 0
    }
    return this.getProductStock(freshProduct)
  },

  updateQuantity(id, quantity) {
    if (quantity <= 0) {
      this.removeFromCart(id)
      return true
    }
    for (let i = 0; i < this.cart.length; i++) {
      if (this.cart[i].id === id) {
        const stock = this.getLiveCartItemStock(this.cart[i])
        this.cart[i].displayStock = stock
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
    const items = this.cart.map(ci => ({
      product_id: ci.product.articul,
      variation_id: ci.productVariation ? ci.productVariation.id : null,
      quantity: ci.quantity,
      price: ci.displayPrice
    }))

    const payload = {
      user_id: this.user ? this.user.userId : null,
      name: customerName || (this.user ? this.user.name : 'Покупатель'),
      phone: customerPhone || (this.user ? this.user.phone : ''),
      email: this.user ? this.user.email : null,
      address,
      items
    }

    const newOrder = await api.createOrder(payload)

    this.orders.unshift(newOrder)
    this.clearCart()

    // Мгновенно подтягиваем списанные остатки из БД во все открытые экраны
    await this.loadFromApi(true)
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
      }
    } catch (e) {
      console.warn('Ошибка загрузки отзывов:', e)
    }
  },

  getRating(articul) {
    const revs = this.getReviews(articul)
    if (revs.length > 0) {
      const sum = revs.reduce((acc, r) => acc + (Number(r.rating) || 0), 0)
      return {
        rating: Math.round((sum / revs.length) * 10) / 10,
        count: revs.length
      }
    }
    const prod = this.products.find(p => String(p.articul) === String(articul))
    return {
      rating: prod && prod.rating != null ? Number(prod.rating) : 5.0,
      count: prod && prod.reviewsCount != null ? Number(prod.reviewsCount) : 0
    }
  },

  async addReview(articul, rating, comment, name) {
    const author = name || (this.user ? this.user.name : 'Покупатель')
    const serverRev = await api.addReview(
      articul,
      author,
      rating,
      comment,
      this.user ? this.user.userId : null
    )

    if (!this.reviews[articul]) {
      this.reviews[articul] = []
    }
    this.reviews[articul].unshift(serverRev)

    // Обновляем товар с сервера, чтобы рейтинг в каталоге совпал с БД
    await this.loadProduct(articul)
    return serverRev
  }
})
