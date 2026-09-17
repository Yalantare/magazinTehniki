import { reactive } from 'vue'
import { products, categories, brands, orders, reviews, user } from './data.js'
import { api } from './api.js'

export const store = reactive({
  user: JSON.parse(localStorage.getItem('user')) || user,
  isGuest: localStorage.getItem('isGuest') === 'true',
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
    } catch (e) {
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

  login(email, pass) {
    this.user = {
      userId: 3,
      role: 'user',
      name: 'Рафаэль Хайруллин',
      phone: '79174948936',
      email: email,
      password: pass
    }
    this.isGuest = false
    localStorage.setItem('user', JSON.stringify(this.user))
    localStorage.setItem('isGuest', 'false')
  },

  loginGuest() {
    this.user = null
    this.isGuest = true
    localStorage.removeItem('user')
    localStorage.setItem('isGuest', 'true')
  },

  register(name, email, phone, pass) {
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

  createOrder(address) {
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

    const newOrder = {
      receiptId: Date.now(),
      code: '#TF-2026-000' + (this.orders.length + 1),
      userId: this.user ? this.user.userId : 3,
      totalPrice: this.getCartTotal(),
      dateTime: new Date().toISOString(),
      statusTitle: 'В обработке',
      address,
      user: this.user,
      receiptItems: items
    }

    this.orders.unshift(newOrder)
    localStorage.setItem('orders', JSON.stringify(this.orders))
    this.clearCart()
    return newOrder
  },

  getReviews(articul) {
    return this.reviews[articul] || []
  },

  getRating(articul) {
    const prod = this.products.find(p => p.articul === articul)
    const revs = this.getReviews(articul)
    if (revs.length === 0) {
      return {
        rating: prod ? prod.rating : 5,
        count: prod ? prod.reviewsCount : 0
      }
    }
    let sum = 0
    for (let i = 0; i < revs.length; i++) {
      sum += revs[i].rating
    }
    const totalCount = (prod ? prod.reviewsCount : 0) + revs.length
    const avg = ((prod ? prod.rating : 5) * (prod ? prod.reviewsCount : 0) + sum) / totalCount
    return {
      rating: Math.round(avg * 10) / 10,
      count: totalCount
    }
  },

  addReview(articul, rating, comment, name) {
    const now = new Date()
    const dateStr = now.toLocaleDateString('ru-RU')
    if (!this.reviews[articul]) {
      this.reviews[articul] = []
    }
    this.reviews[articul].unshift({
      id: 'rev-' + Date.now(),
      articul,
      userName: name || 'Покупатель',
      rating,
      date: dateStr,
      comment
    })
    localStorage.setItem('reviews', JSON.stringify(this.reviews))
  }
})
