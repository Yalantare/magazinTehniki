export const categories = [
  { id: 1, title: 'Наушники' },
  { id: 2, title: 'Ноутбуки' },
  { id: 3, title: 'Телефон' },
  { id: 4, title: 'Часы' }
]

export const brands = ['Apple', 'Huawei', 'Samsung', 'Xiaomi']

export const products = [
  {
    articul: 1,
    title: 'AppleWatch 16',
    manufacturer: 'Apple',
    category: 4,
    price: 40000,
    stock: 0,
    rating: 4.8,
    reviewsCount: 12,
    description: 'Умные часы AppleWatch 16 с передовыми датчиками для заботы о здоровье, ярким OLED Always-On дисплеем и прочным корпусом для любых тренировок.',
    photo: '/images/apple_watch.jpeg',
    categoryNavigation: { id: 4, title: 'Часы' }
  },
  {
    articul: 2,
    title: 'MacBook Pro 16',
    manufacturer: 'Apple',
    category: 2,
    price: 249999,
    stock: 5,
    rating: 5.0,
    reviewsCount: 18,
    description: 'Ноутбук Apple MacBook Pro 16 с потрясающим дисплеем Liquid Retina XDR, высокой производительностью процессоров Apple M-серии и непревзойденным временем автономной работы.',
    photo: '/images/7302914176.jpg',
    categoryNavigation: { id: 2, title: 'Ноутбуки' }
  },
  {
    articul: 3,
    title: 'AirPods Pro 3',
    manufacturer: 'Apple',
    category: 1,
    price: 24990,
    stock: 30,
    rating: 4.9,
    reviewsCount: 26,
    description: 'Беспроводные наушники AirPods Pro 3 с передовым активным шумоподавлением, режимом адаптивной прозрачности и персонализированным пространственным звуком.',
    photo: '/images/s-l1600.jpg',
    categoryNavigation: { id: 1, title: 'Наушники' }
  },
  {
    articul: 4,
    title: 'iPhone 15 Pro',
    manufacturer: 'Apple',
    category: 3,
    price: 129990,
    stock: 8,
    rating: 4.9,
    reviewsCount: 35,
    description: 'Корпус из авиационного титана, мощнейший процессор A17 Pro, настраиваемая кнопка действия Action Button и универсальный порт USB-C для максимальной скорости передачи данных.',
    photo: '/images/iphone_15_pro.jpg',
    categoryNavigation: { id: 3, title: 'Телефон' },
    productVariations: [
      { id: 1, productId: 4, name: '128 GB', price: 129990, stock: 4 },
      { id: 2, productId: 4, name: '256 GB', price: 144990, stock: 3 },
      { id: 3, productId: 4, name: '512 GB', price: 169990, stock: 1 }
    ]
  },
  {
    articul: 5,
    title: 'Xiaomi Ultra 17',
    manufacturer: 'Xiaomi',
    category: 3,
    price: 75000,
    stock: 3,
    rating: 4.9,
    reviewsCount: 21,
    description: 'Флагманский смартфон Xiaomi Ultra 17 с профессиональной оптикой Leica, ультрачетким AMOLED-дисплеем и молниеносной зарядкой.',
    photo: '/images/iauk5enkbbqmdfijupnwve25fan6hpdz.jpg',
    categoryNavigation: { id: 3, title: 'Телефон' },
    productVariations: [
      { id: 4, productId: 5, name: '256 GB (Изумрудный)', price: 75000, stock: 2 },
      { id: 5, productId: 5, name: '512 GB (Изумрудный)', price: 85000, stock: 1 }
    ]
  },
  {
    articul: 6,
    title: 'Samsung Galaxy S24 Ultra',
    manufacturer: 'Samsung',
    category: 3,
    price: 119990,
    stock: 10,
    rating: 4.8,
    reviewsCount: 19,
    description: 'Инновационный смартфон со встроенным пером S Pen, интеллектуальными возможностями Galaxy, титановым корпусом и камерой 200 Мп с непревзойденным ночным зумом.',
    photo: '/images/l9mlom3hkqe3dl1mwpjkdamxyzar55y4.jpg',
    categoryNavigation: { id: 3, title: 'Телефон' }
  },
  {
    articul: 7,
    title: 'Huawei FreeBuds Pro 3',
    manufacturer: 'Huawei',
    category: 1,
    price: 14990,
    stock: 12,
    rating: 4.7,
    reviewsCount: 14,
    description: 'Наушники премиального уровня с двумя излучателями высокого разрешения, кристально чистой передачей голоса и интеллектуальным ANC.',
    photo: '/images/edbd519128c26b1de9ba7b3cdfd827e8.jpg',
    categoryNavigation: { id: 1, title: 'Наушники' }
  },
  {
    articul: 8,
    title: 'Huawei Watch GT 4',
    manufacturer: 'Huawei',
    category: 4,
    price: 19990,
    stock: 7,
    rating: 4.8,
    reviewsCount: 16,
    description: 'Элегантные часы в геометрическом дизайне с автономностью до 14 дней, круглосуточным контролем здоровья и совместимостью со всеми ОС.',
    photo: '/images/AA1T0iYZ.jfif',
    categoryNavigation: { id: 4, title: 'Часы' }
  }
]

export const reviews = {
  5: [
    {
      id: 'rev-5-1',
      articul: 5,
      userName: 'Алексей С.',
      rating: 5,
      date: '02.09.2026',
      comment: 'Камера Leica просто невероятная! Цветопередача и детализация на высшем уровне. Батарею держит полтора дня стабильно.'
    },
    {
      id: 'rev-5-2',
      articul: 5,
      userName: 'Марина К.',
      rating: 5,
      date: '28.08.2026',
      comment: 'Очень красивый изумрудный цвет корпуса. Быстрая зарядка заряжает до 100% за какие-то 25 минут!'
    },
    {
      id: 'rev-5-3',
      articul: 5,
      userName: 'Денис В.',
      rating: 4,
      date: '15.08.2026',
      comment: 'Смартфон топовый, экран 120 Гц суперплавный. Из минусов: блок камер ощутимо выступает, лучше сразу брать чехол.'
    }
  ],
  4: [
    {
      id: 'rev-4-1',
      articul: 4,
      userName: 'Артур Г.',
      rating: 5,
      date: '04.09.2026',
      comment: 'Титан ощущается намного легче стали. Type-C наконец-то позволяет заряжать одним проводом и ноутбук, и телефон.'
    },
    {
      id: 'rev-4-2',
      articul: 4,
      userName: 'Елена М.',
      rating: 5,
      date: '20.08.2026',
      comment: 'Камера с 5х зумом творит чудеса. Производительность в играх и тяжелых приложениях космическая.'
    }
  ],
  3: [
    {
      id: 'rev-3-1',
      articul: 3,
      userName: 'Сергей Т.',
      rating: 5,
      date: '05.09.2026',
      comment: 'Шумоподавление лучше, чем во второй версии. В метро тишина полная, звук насыщенный с глубокими басами.'
    },
    {
      id: 'rev-3-2',
      articul: 3,
      userName: 'Ольга Р.',
      rating: 5,
      date: '01.09.2026',
      comment: 'Сидят идеально, не выпадают даже на пробежках. Автономность отличная.'
    }
  ],
  2: [
    {
      id: 'rev-2-1',
      articul: 2,
      userName: 'Владимир П.',
      rating: 5,
      date: '03.09.2026',
      comment: 'Рабочая машина мечты. Рендер 4K видео без единого звука вентиляторов. Дисплей 120 Гц XDR просто сказка.'
    }
  ]
}

export const user = {
  userId: 3,
  role: 'user',
  name: 'Рафаэль Хайруллин',
  phone: '79174948936',
  email: 'hayrullinrafael2@gmail.com',
  password: 'password123'
}

export const orders = [
  {
    receiptId: 4,
    code: '#TF-2024-00004',
    userId: 3,
    totalPrice: 749700,
    dateTime: '2026-05-24T17:03:00',
    statusTitle: 'Создан и оплачен',
    address: 'г. Уфа, ул. Кирова, д. 65/2, кв. 1',
    user: user,
    receiptItems: [
      {
        id: 101,
        quantity: 5,
        priceAtPurchase: 129990,
        product: products[3]
      },
      {
        id: 102,
        quantity: 4,
        priceAtPurchase: 24990,
        product: products[2]
      }
    ]
  },
  {
    receiptId: 3,
    code: '#TF-2024-00003',
    userId: 3,
    totalPrice: 409980,
    dateTime: '2026-05-24T12:15:00',
    statusTitle: 'В обработке',
    address: 'г. Уфа, ул. Кирова, д. 65/2, кв. 1',
    user: user,
    receiptItems: [
      {
        id: 103,
        quantity: 1,
        priceAtPurchase: 249999,
        product: products[1]
      },
      {
        id: 104,
        quantity: 1,
        priceAtPurchase: 159990,
        product: products[3]
      }
    ]
  },
  {
    receiptId: 2,
    code: '#TF-2024-00002',
    userId: 3,
    totalPrice: 199990,
    dateTime: '2026-05-23T09:40:00',
    statusTitle: 'Доставлен',
    address: 'г. Уфа, ул. Кирова, д. 65/2, кв. 1',
    user: user,
    receiptItems: [
      {
        id: 105,
        quantity: 1,
        priceAtPurchase: 119990,
        product: products[5]
      },
      {
        id: 106,
        quantity: 1,
        priceAtPurchase: 75000,
        product: products[4]
      }
    ]
  }
]
