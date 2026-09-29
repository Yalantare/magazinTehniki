import os
import sys
import time
import shutil
from sqlmodel import SQLModel, create_engine, Session, select

# Функция загрузки переменных из .env с поддержкой python-dotenv и fallback
def _load_env():
    possible_paths = [
        os.path.join(os.getcwd(), ".env"),
        os.path.join(os.path.dirname(os.path.abspath(__file__)), ".env"),
        os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), ".env"),
    ]
    try:
        from dotenv import load_dotenv
        for path in possible_paths:
            if os.path.isfile(path):
                load_dotenv(path)
                break
    except ImportError:
        for path in possible_paths:
            if os.path.isfile(path):
                try:
                    with open(path, "r", encoding="utf-8") as f:
                        for line in f:
                            line = line.strip()
                            if line and not line.startswith("#") and "=" in line:
                                k, v = line.split("=", 1)
                                k = k.strip()
                                v = v.strip().strip("'").strip('"')
                                if k not in os.environ:
                                    os.environ[k] = v
                    break
                except Exception:
                    pass

_load_env()

# Параметры подключения
DB_USER = os.getenv("DB_USER", "root")
DB_PASSWORD = os.getenv("DB_PASSWORD", "root")
DB_HOST = os.getenv("DB_HOST", "localhost")
DB_PORT = os.getenv("DB_PORT", "3307")
DB_NAME = os.getenv("DB_NAME", "shop")

DATABASE_URL = os.getenv(
    "DATABASE_URL",
    f"mysql+pymysql://{DB_USER}:{DB_PASSWORD}@{DB_HOST}:{DB_PORT}/{DB_NAME}?charset=utf8mb4"
)

engine = create_engine(
    DATABASE_URL,
    pool_pre_ping=True,
    pool_recycle=3600,
    echo=False
)

def _sync_product_images():
    """Копирует исходные изображения из клиентской папки в api/images при их отсутствии."""
    try:
        api_dir = os.path.dirname(os.path.abspath(__file__))
        images_dir = os.path.join(api_dir, "images")
        os.makedirs(images_dir, exist_ok=True)

        client_images_dir = os.path.join(os.path.dirname(api_dir), "client", "public", "images")
        if os.path.isdir(client_images_dir):
            for file_name in os.listdir(client_images_dir):
                src = os.path.join(client_images_dir, file_name)
                dst = os.path.join(images_dir, file_name)
                if os.path.isfile(src) and not os.path.exists(dst):
                    shutil.copy2(src, dst)
    except Exception as e:
        print(f"[DB] Ошибка синхронизации изображений: {e}")

def _migrate_schema(db_engine):
    """Автоматически приводит схему существующих таблиц БД в соответствие с 3NF."""
    try:
        from sqlalchemy import inspect, text
        inspector = inspect(db_engine)
        existing_tables = inspector.get_table_names()
        with db_engine.begin() as conn:
            if "roles" in existing_tables:
                conn.execute(text("INSERT IGNORE INTO roles (id, name) VALUES (1, 'user'), (2, 'admin')"))
            if "manufacturers" in existing_tables:
                conn.execute(text("INSERT IGNORE INTO manufacturers (id, name) VALUES (1, 'Apple'), (2, 'Xiaomi'), (3, 'Samsung'), (4, 'Huawei')"))
            if "users" in existing_tables:
                user_cols = [c["name"] for c in inspector.get_columns("users")]
                if "role_id" not in user_cols:
                    conn.execute(text("ALTER TABLE users ADD COLUMN role_id INT DEFAULT 1"))
                    if "role" in user_cols:
                        conn.execute(text("UPDATE users SET role_id = 2 WHERE role = 'admin'"))
            if "products" in existing_tables:
                prod_cols = [c["name"] for c in inspector.get_columns("products")]
                if "category_id" not in prod_cols:
                    conn.execute(text("ALTER TABLE products ADD COLUMN category_id INT"))
                    if "category" in prod_cols:
                        conn.execute(text("UPDATE products SET category_id = category WHERE category IS NOT NULL"))
                if "manufacturer_id" not in prod_cols:
                    conn.execute(text("ALTER TABLE products ADD COLUMN manufacturer_id INT"))
                    if "manufacturer" in prod_cols:
                        conn.execute(text("UPDATE products p JOIN manufacturers m ON LOWER(TRIM(p.manufacturer)) = LOWER(TRIM(m.name)) SET p.manufacturer_id = m.id"))
                    conn.execute(text("UPDATE products SET manufacturer_id = 1 WHERE manufacturer_id IS NULL"))
            if "receipts" in existing_tables:
                rec_cols = [c["name"] for c in inspector.get_columns("receipts")]
                if "status_id" not in rec_cols:
                    conn.execute(text("ALTER TABLE receipts ADD COLUMN status_id INT DEFAULT 1"))
                    if "status" in rec_cols:
                        conn.execute(text("UPDATE receipts SET status_id = status WHERE status IS NOT NULL"))
            if "reviews" in existing_tables:
                rev_cols = [c["name"] for c in inspector.get_columns("reviews")]
                if "user_id" not in rev_cols:
                    conn.execute(text("ALTER TABLE reviews ADD COLUMN user_id INT NULL"))
    except Exception as e:
        print(f"[DB] Предупреждение при проверке/миграции схемы: {e}")

def _seed_initial_data(session: Session, models_module):
    """Наполняет базу данных начальными данными, если таблицы пусты."""
    try:
        # 1. Роли
        roles = session.exec(select(models_module.Role)).all()
        if not roles:
            initial_roles = [
                models_module.Role(id=1, name="user"),
                models_module.Role(id=2, name="admin"),
            ]
            session.add_all(initial_roles)
            session.commit()
            print("[DB] Начальные роли успешно добавлены.")

        # 2. Производители
        manufacturers = session.exec(select(models_module.Manufacturer)).all()
        if not manufacturers:
            initial_manufacturers = [
                models_module.Manufacturer(id=1, name="Apple"),
                models_module.Manufacturer(id=2, name="Xiaomi"),
                models_module.Manufacturer(id=3, name="Samsung"),
                models_module.Manufacturer(id=4, name="Huawei"),
            ]
            session.add_all(initial_manufacturers)
            session.commit()
            print("[DB] Начальные производители успешно добавлены.")

        # 3. Статусы заказов
        statuses = session.exec(select(models_module.Status)).all()
        if not statuses:
            initial_statuses = [
                models_module.Status(id=1, title="В обработке"),
                models_module.Status(id=2, title="Оплачен"),
                models_module.Status(id=3, title="В доставке"),
                models_module.Status(id=4, title="Доставлен"),
                models_module.Status(id=5, title="Отменен"),
            ]
            session.add_all(initial_statuses)
            session.commit()
            print("[DB] Начальные статусы заказов успешно добавлены.")

        # 4. Категории товаров
        categories = session.exec(select(models_module.Category)).all()
        if not categories:
            initial_categories = [
                models_module.Category(id=1, title="Наушники"),
                models_module.Category(id=2, title="Ноутбуки"),
                models_module.Category(id=3, title="Телефон"),
                models_module.Category(id=4, title="Часы"),
            ]
            session.add_all(initial_categories)
            session.commit()
            print("[DB] Начальные категории успешно добавлены.")

        # 5. Пользователи (Администратор и демо-клиент)
        admin_user = session.exec(
            select(models_module.User).where(models_module.User.email == "admin@shop.ru")
        ).first()
        if not admin_user:
            admin_user = models_module.User(
                user_id=1,
                role_id=2,
                name="Администратор",
                phone="+79990000000",
                password="admin",
                email="admin@shop.ru"
            )
            session.add(admin_user)
            session.commit()
            print("[DB] Создан администратор по умолчанию (admin@shop.ru / admin).")

        demo_user = session.exec(
            select(models_module.User).where(models_module.User.email == "hayrullinrafael2@gmail.com")
        ).first()
        if not demo_user:
            demo_user = models_module.User(
                user_id=2,
                role_id=1,
                name="Рафаэль Хайруллин",
                phone="79174948936",
                password="password123",
                email="hayrullinrafael2@gmail.com"
            )
            session.add(demo_user)
            session.commit()
            print("[DB] Создан тестовый пользователь (hayrullinrafael2@gmail.com / password123).")

        # 6. Товары и вариации
        products = session.exec(select(models_module.Product)).all()
        if not products:
            initial_products = [
                models_module.Product(
                    articul=1,
                    title="AppleWatch 16",
                    manufacturer_id=1,
                    category_id=4,
                    price=40000.0,
                    stock=5,
                    description="Умные часы AppleWatch 16 с передовыми датчиками для заботы о здоровье, ярким OLED Always-On дисплеем и прочным корпусом для любых тренировок.",
                    photo="/images/apple_watch.jpeg"
                ),
                models_module.Product(
                    articul=2,
                    title="MacBook Pro 16",
                    manufacturer_id=1,
                    category_id=2,
                    price=249999.0,
                    stock=5,
                    description="Ноутбук Apple MacBook Pro 16 с потрясающим дисплеем Liquid Retina XDR, высокой производительностью процессоров Apple M-серии и непревзойденным временем автономной работы.",
                    photo="/images/7302914176.jpg"
                ),
                models_module.Product(
                    articul=3,
                    title="AirPods Pro 3",
                    manufacturer_id=1,
                    category_id=1,
                    price=24990.0,
                    stock=30,
                    description="Беспроводные наушники AirPods Pro 3 с передовым активным шумоподавлением, режимом адаптивной прозрачности и персонализированным пространственным звуком.",
                    photo="/images/s-l1600.jpg"
                ),
                models_module.Product(
                    articul=4,
                    title="iPhone 15 Pro",
                    manufacturer_id=1,
                    category_id=3,
                    price=129990.0,
                    stock=8,
                    description="Корпус из авиационного титана, мощнейший процессор A17 Pro, настраиваемая кнопка действия Action Button и универсальный порт USB-C для максимальной скорости передачи данных.",
                    photo="/images/iphone_15_pro.jpg"
                ),
                models_module.Product(
                    articul=5,
                    title="Xiaomi Ultra 17",
                    manufacturer_id=2,
                    category_id=3,
                    price=75000.0,
                    stock=3,
                    description="Флагманский смартфон Xiaomi Ultra 17 с профессиональной оптикой Leica, ультрачетким AMOLED-дисплеем и молниеносной зарядкой.",
                    photo="/images/iauk5enkbbqmdfijupnwve25fan6hpdz.jpg"
                ),
                models_module.Product(
                    articul=6,
                    title="Samsung Galaxy S24 Ultra",
                    manufacturer_id=3,
                    category_id=3,
                    price=119990.0,
                    stock=10,
                    description="Инновационный смартфон со встроенным пером S Pen, интеллектуальными возможностями Galaxy, титановым корпусом и камерой 200 Мп с непревзойденным ночным зумом.",
                    photo="/images/l9mlom3hkqe3dl1mwpjkdamxyzar55y4.jpg"
                ),
                models_module.Product(
                    articul=7,
                    title="Huawei FreeBuds Pro 3",
                    manufacturer_id=4,
                    category_id=1,
                    price=14990.0,
                    stock=12,
                    description="Наушники премиального уровня с двумя излучателями высокого разрешения, кристально чистой передачей голоса и интеллектуальным ANC.",
                    photo="/images/edbd519128c26b1de9ba7b3cdfd827e8.jpg"
                ),
                models_module.Product(
                    articul=8,
                    title="Huawei Watch GT 4",
                    manufacturer_id=4,
                    category_id=4,
                    price=19990.0,
                    stock=7,
                    description="Элегантные часы в геометрическом дизайне с автономностью до 14 дней, круглосуточным контролем здоровья и совместимостью со всеми ОС.",
                    photo="/images/AA1T0iYZ.jfif"
                ),
            ]
            session.add_all(initial_products)
            session.commit()

            initial_variations = [
                models_module.ProductVariation(id=1, product_id=4, name="128 GB", price=129990.0, stock=4),
                models_module.ProductVariation(id=2, product_id=4, name="256 GB", price=144990.0, stock=3),
                models_module.ProductVariation(id=3, product_id=4, name="512 GB", price=169990.0, stock=1),
                models_module.ProductVariation(id=4, product_id=5, name="256 GB (Изумрудный)", price=75000.0, stock=2),
                models_module.ProductVariation(id=5, product_id=5, name="512 GB (Изумрудный)", price=85000.0, stock=1),
            ]
            session.add_all(initial_variations)
            session.commit()
            print("[DB] Начальные товары и вариации успешно добавлены.")

        # 7. Отзывы
        reviews = session.exec(select(models_module.Review)).all()
        if not reviews:
            initial_reviews = [
                models_module.Review(articul=5, user_name="Алексей С.", rating=5, date="02.09.2026", comment="Камера Leica просто невероятная! Цветопередача и детализация на высшем уровне."),
                models_module.Review(articul=4, user_name="Артур Г.", rating=5, date="04.09.2026", comment="Титан ощущается намного легче стали. Type-C наконец-то позволяет заряжать одним проводом."),
                models_module.Review(articul=3, user_name="Сергей Т.", rating=5, date="05.09.2026", comment="Шумоподавление лучше, чем во второй версии. В метро тишина полная."),
                models_module.Review(articul=2, user_name="Владимир П.", rating=5, date="03.09.2026", comment="Рабочая машина мечты. Рендер 4K видео без единого звука вентиляторов."),
                models_module.Review(articul=1, user_name="Евгений Д.", rating=5, date="01.09.2026", comment="Отличные часы, очень стильные и функциональные! Батарею держат весь день."),
            ]
            session.add_all(initial_reviews)
            session.commit()
            print("[DB] Начальные отзывы добавлены.")

    except Exception as e:
        session.rollback()
        print(f"[DB] Ошибка при заполнении начальных данных: {e}")

def init_db(retries: int = 5, delay: float = 2.0):
    """Инициализация базы данных: создание таблиц и добавление начальных данных."""
    # Импортируем модели так, чтобы они зарегистрировались в метаданных SQLModel
    current_dir = os.path.dirname(os.path.abspath(__file__))
    if current_dir not in sys.path:
        sys.path.insert(0, current_dir)

    try:
        import models as models_module
    except ImportError:
        from api import models as models_module

    for attempt in range(1, retries + 1):
        try:
            print(f"[DB] Подключение к базе данных ({DB_HOST}:{DB_PORT}/{DB_NAME}), попытка {attempt}/{retries}...")
            SQLModel.metadata.create_all(engine)
            _migrate_schema(engine)
            print("[DB] Таблицы базы данных успешно созданы или проверены.")
            
            with Session(engine) as session:
                _seed_initial_data(session, models_module)

            _sync_product_images()
            print("[DB] Инициализация базы данных успешно завершена!")
            return True
        except Exception as e:
            print(f"[DB] Ошибка подключения на попытке {attempt}: {e}")
            if attempt < retries:
                time.sleep(delay)
            else:
                print("[DB] Внимание: не удалось подключиться к базе данных. Проверьте, запущена ли СУБД MySQL.")
                return False

def get_session():
    """Генератор сессии базы данных для FastAPI Depends."""
    with Session(engine) as session:
        yield session

if __name__ == "__main__":
    init_db()
