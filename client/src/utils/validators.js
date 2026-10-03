/**
 * Утилиты валидации и форматирования полей ввода
 */

/**
 * Блокировка ввода лишних пробелов на событии @keydown
 * @param {KeyboardEvent} e - событие клавиатуры
 * @param {string} value - текущее значение в поле
 * @param {number} maxSpaces - максимально допустимое число пробелов (0 = запрет)
 */
export function handleSpaceKeydown(e, value, maxSpaces = 0) {
  if (e.key === ' ' || e.code === 'Space') {
    if (maxSpaces <= 0) {
      e.preventDefault()
      return
    }
    const str = String(value || '')
    // Запрет пробела в самом начале
    const target = e.target
    const pos = target && typeof target.selectionStart === 'number' ? target.selectionStart : str.length
    if (pos === 0) {
      e.preventDefault()
      return
    }
    // Запрет двух пробелов подряд
    if (str[pos - 1] === ' ' || str[pos] === ' ') {
      e.preventDefault()
      return
    }
    // Проверка общего количества пробелов
    const currentSpaces = (str.match(/ /g) || []).length
    if (currentSpaces >= maxSpaces) {
      e.preventDefault()
      return
    }
  }
}

/**
 * Очистка пароля: строго без пробелов, максимум maxLen (по умолчанию 32)
 */
export function sanitizePassword(val, maxLen = 32) {
  if (!val) return ''
  return String(val).replace(/\s+/g, '').slice(0, maxLen)
}

/**
 * Очистка Email: без пробелов, ограничение по длине
 */
export function sanitizeEmail(val, maxLen = 64) {
  if (!val) return ''
  return String(val).replace(/\s+/g, '').slice(0, maxLen)
}

/**
 * Проверка корректности Email
 */
export function isValidEmail(email) {
  const re = /^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$/
  return re.test(String(email || '').trim())
}

/**
 * Маска для номера телефона: +7 (XXX) XXX-XX-XX
 */
export function formatPhone(val) {
  if (!val) return ''
  let digits = String(val).replace(/\D/g, '')
  if (!digits) return ''

  if (digits[0] === '8' || digits[0] === '7') {
    digits = digits.slice(1)
  }
  digits = digits.slice(0, 10)

  let res = '+7'
  if (digits.length > 0) {
    res += ' (' + digits.slice(0, 3)
  }
  if (digits.length >= 3) {
    res += ') '
  }
  if (digits.length > 3) {
    res += digits.slice(3, 6)
  }
  if (digits.length > 6) {
    res += '-' + digits.slice(6, 8)
  }
  if (digits.length > 8) {
    res += '-' + digits.slice(8, 10)
  }

  return res
}

/**
 * Проверка валидности телефона (+7 и 10 цифр)
 */
export function isValidPhone(val) {
  const digits = String(val || '').replace(/\D/g, '')
  return digits.length === 11 && (digits[0] === '7' || digits[0] === '8')
}

/**
 * Очистка имени/ФИО:
 * - без повторяющихся пробелов
 * - без пробела в начале
 * - не более maxSpaces пробелов (по умолчанию 2 для "Фамилия Имя Отчество")
 * - длина до maxLen (по умолчанию 60)
 */
export function sanitizeName(val, maxSpaces = 2, maxLen = 60) {
  if (!val) return ''
  let str = String(val).replace(/^\s+/, '').replace(/\s{2,}/g, ' ')
  const parts = str.split(' ')
  if (parts.length > maxSpaces + 1) {
    str = parts.slice(0, maxSpaces + 1).join(' ')
  }
  return str.slice(0, maxLen)
}

/**
 * Общая очистка текста с ограничением пробелов и длины
 */
export function sanitizeTextWithSpaces(val, maxSpaces = 3, maxLen = 80) {
  if (!val) return ''
  let str = String(val).replace(/^\s+/, '').replace(/\s{2,}/g, ' ')
  const parts = str.split(' ')
  if (parts.length > maxSpaces + 1) {
    str = parts.slice(0, maxSpaces + 1).join(' ')
  }
  return str.slice(0, maxLen)
}

/**
 * Очистка строк с цифрами (индексы, номера дома, этажи, квартиры)
 */
export function sanitizeDigits(val, maxLen = 10) {
  if (!val) return ''
  return String(val).replace(/\D/g, '').slice(0, maxLen)
}

/**
 * Форматирование номера банковской карты (16 цифр по 4)
 */
export function sanitizeCardNumber(val) {
  if (!val) return ''
  const digits = String(val).replace(/\D/g, '').slice(0, 16)
  const parts = []
  for (let i = 0; i < digits.length; i += 4) {
    parts.push(digits.slice(i, i + 4))
  }
  return parts.join(' ')
}

/**
 * Форматирование срока карты MM/YY
 */
export function sanitizeCardExpiry(val) {
  if (!val) return ''
  const digits = String(val).replace(/\D/g, '').slice(0, 4)
  if (digits.length <= 2) return digits
  return digits.slice(0, 2) + '/' + digits.slice(2, 4)
}

/**
 * Очистка CVV (3 цифры, 0 пробелов)
 */
export function sanitizeCardCvv(val) {
  if (!val) return ''
  return String(val).replace(/\D/g, '').slice(0, 3)
}
