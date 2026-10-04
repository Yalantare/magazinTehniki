import os
import sys
from datetime import datetime
from typing import Optional, List
from contextlib import asynccontextmanager
from fastapi import FastAPI, Depends, HTTPException, Body, Request
from openai import AsyncOpenAI
from pydantic import BaseModel
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from sqlmodel import Session, select, col

current_dir = os.path.dirname(os.path.abspath(__file__))
if current_dir not in sys.path:
    sys.path.insert(0, current_dir)

try:
    from . import database, models
except (ImportError, ValueError):
    import database
    import models

if "api.models" in sys.modules and "models" not in sys.modules:
    sys.modules["models"] = sys.modules["api.models"]
elif "models" in sys.modules and "api.models" not in sys.modules:
    sys.modules["api.models"] = sys.modules["models"]

if "api.database" in sys.modules and "database" not in sys.modules:
    sys.modules["database"] = sys.modules["api.database"]
elif "database" in sys.modules and "api.database" not in sys.modules:
    sys.modules["api.database"] = sys.modules["database"]

@asynccontextmanager
async def lifespan(app: FastAPI):
    database.init_db()
    yield

app = FastAPI(title="Магазин техники API", lifespan=lifespan)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

images_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "images")
os.makedirs(images_dir, exist_ok=True)
app.mount("/images", StaticFiles(directory=images_dir), name="images")

def get_or_create_manufacturer(name_or_id, session: Session) -> int:
    if isinstance(name_or_id, int):
        return name_or_id
    if not name_or_id:
        first = session.exec(select(models.Manufacturer)).first()
        if first and first.id is not None:
            return first.id
        mfg = models.Manufacturer(name="Unknown")
        session.add(mfg)
        session.commit()
        session.refresh(mfg)
        return mfg.id or 0

    name = str(name_or_id).strip()
    mfg = session.exec(select(models.Manufacturer).where(col(models.Manufacturer.name).ilike(name))).first()
    if not mfg:
        mfg = models.Manufacturer(name=name)
        session.add(mfg)
        session.commit()
        session.refresh(mfg)
    return mfg.id or 0

def get_or_create_role(name_or_id, session: Session) -> int:
    if isinstance(name_or_id, int):
        return name_or_id
    role_name = (name_or_id or "user").strip().lower()
    role = session.exec(select(models.Role).where(models.Role.name == role_name)).first()
    if not role:
        role = models.Role(name=role_name)
        session.add(role)
        session.commit()
        session.refresh(role)
    return role.id or 1

def format_user_response(u: models.User, session: Session):
    role = session.get(models.Role, u.role_id) if u.role_id else None
    role_name = role.name if role else "user"
    return {
        "userId": u.user_id,
        "role": role_name,
        "roleId": u.role_id,
        "name": u.name,
        "phone": u.phone,
        "email": u.email
    }

def format_product(p: models.Product, session: Session):
    cat = session.get(models.Category, p.category_id) if p.category_id else None
    mfg = session.get(models.Manufacturer, p.manufacturer_id) if p.manufacturer_id else None
    variations = session.exec(
        select(models.ProductVariation).where(models.ProductVariation.product_id == p.articul)
    ).all()

    reviews = session.exec(
        select(models.Review).where(models.Review.product_id == p.articul)
    ).all()
    rev_count = len(reviews)
    avg_rating = round(sum(r.rating for r in reviews) / rev_count, 1) if rev_count > 0 else 5.0

    return {
        "articul": p.articul,
        "title": p.title,
        "manufacturer": mfg.name if mfg else "",
        "manufacturerId": p.manufacturer_id,
        "manufacturerNavigation": {
            "id": mfg.id,
            "name": mfg.name
        } if mfg else None,
        "category": p.category_id,
        "categoryId": p.category_id,
        "price": p.price,
        "stock": p.stock,
        "rating": avg_rating,
        "reviewsCount": rev_count,
        "description": p.description,
        "photo": p.photo,
        "categoryNavigation": {
            "id": cat.id,
            "title": cat.title
        } if cat else None,
        "productVariations": [
            {
                "id": v.id,
                "productId": v.product_id,
                "name": v.name,
                "price": v.price,
                "stock": v.stock
            } for v in variations
        ]
    }

def format_receipt(r: models.Receipt, session: Session):
    items = session.exec(
        select(models.ReceiptItem).where(models.ReceiptItem.receipt_id == r.receipt_id)
    ).all()

    formatted_items = []
    for item in items:
        prod = session.get(models.Product, item.product_id)
        prod_data = format_product(prod, session) if prod else None

        var_data = None
        if item.variation_id:
            v = session.get(models.ProductVariation, item.variation_id)
            if v:
                var_data = {
                    "id": v.id,
                    "productId": v.product_id,
                    "name": v.name,
                    "price": v.price,
                    "stock": v.stock
                }

        formatted_items.append({
            "id": item.id,
            "receiptId": item.receipt_id,
            "productId": item.product_id,
            "variationId": item.variation_id,
            "quantity": item.quantity,
            "priceAtPurchase": item.price_at_purchase,
            "product": prod_data,
            "productVariation": var_data
        })

    user = session.get(models.User, r.user_id) if r.user_id else None
    user_data = format_user_response(user, session) if user else None

    status_obj = session.get(models.Status, r.status_id) if r.status_id else None
    status_title = status_obj.title if status_obj else "В обработке"

    return {
        "receiptId": r.receipt_id,
        "code": r.code,
        "userId": r.user_id,
        "totalPrice": r.total_price,
        "dateTime": r.date_time,
        "status": r.status_id,
        "statusId": r.status_id,
        "statusTitle": status_title,
        "orderStatus": r.order_status,
        "address": r.address,
        "adress": r.address,
        "statusNavigation": {
            "id": status_obj.id if status_obj else r.status_id,
            "title": status_title
        },
        "user": user_data,
        "receiptItems": formatted_items
    }

@app.get("/api/products")
@app.get("/api/Products")
def get_products(
    categoryId: Optional[int] = None,
    manufacturerId: Optional[int] = None,
    brand: Optional[str] = None,
    search: Optional[str] = None,
    minPrice: Optional[float] = None,
    maxPrice: Optional[float] = None,
    inStock: Optional[bool] = None,
    session: Session = Depends(database.get_session)
):
    stmt = select(models.Product)
    if categoryId is not None:
        stmt = stmt.where(models.Product.category_id == categoryId)
    if manufacturerId is not None:
        stmt = stmt.where(models.Product.manufacturer_id == manufacturerId)
    if brand:
        stmt = stmt.join(
            models.Manufacturer,
            col(models.Product.manufacturer_id) == models.Manufacturer.id,
            isouter=True
        ).where(col(models.Manufacturer.name).ilike(f"%{brand}%"))
    if search:
        stmt = stmt.where(col(models.Product.title).ilike(f"%{search}%"))
    if minPrice is not None:
        stmt = stmt.where(models.Product.price >= minPrice)
    if maxPrice is not None:
        stmt = stmt.where(models.Product.price <= maxPrice)
    if inStock is True:
        stmt = stmt.where(models.Product.stock > 0)

    products = session.exec(stmt).all()
    return [format_product(p, session) for p in products]

@app.get("/api/products/{articul}")
@app.get("/api/Products/{articul}")
def get_product(articul: int, session: Session = Depends(database.get_session)):
    product = session.get(models.Product, articul)
    if not product:
        raise HTTPException(status_code=404, detail="Товар не найден")
    return format_product(product, session)

@app.post("/api/products")
@app.post("/api/Products")
def create_product(data: models.ProductCreate, session: Session = Depends(database.get_session)):
    mfg_id = data.manufacturer_id
    if not mfg_id and data.manufacturer:
        mfg_id = get_or_create_manufacturer(data.manufacturer, session)
    if not mfg_id:
        mfg_id = get_or_create_manufacturer("Unknown", session)

    cat_id = data.category_id or data.category or 1

    product = models.Product(
        title=data.title,
        manufacturer_id=mfg_id,
        category_id=cat_id,
        price=data.price,
        stock=data.stock,
        description=data.description or "",
        photo=data.photo or ""
    )
    session.add(product)
    session.commit()
    session.refresh(product)
    return format_product(product, session)

@app.put("/api/products/{articul}")
@app.put("/api/Products/{articul}")
def update_product(articul: int, data: models.ProductUpdate, session: Session = Depends(database.get_session)):
    product = session.get(models.Product, articul)
    if not product:
        raise HTTPException(status_code=404, detail="Товар не найден")

    if data.title is not None:
        product.title = data.title
    if data.manufacturer_id is not None:
        product.manufacturer_id = data.manufacturer_id
    elif data.manufacturer is not None:
        product.manufacturer_id = get_or_create_manufacturer(data.manufacturer, session)
    if data.category_id is not None:
        product.category_id = data.category_id
    elif data.category is not None:
        product.category_id = data.category
    if data.price is not None:
        product.price = data.price
    if data.stock is not None:
        product.stock = data.stock
    if data.description is not None:
        product.description = data.description
    if data.photo is not None:
        product.photo = data.photo

    session.add(product)
    session.commit()
    session.refresh(product)
    return format_product(product, session)

@app.delete("/api/products/{articul}")
@app.delete("/api/Products/{articul}")
def delete_product(articul: int, session: Session = Depends(database.get_session)):
    product = session.get(models.Product, articul)
    if not product:
        raise HTTPException(status_code=404, detail="Товар не найден")
    session.delete(product)
    session.commit()
    return {"message": "Товар успешно удален"}

@app.put("/api/products/{articul}/stock")
def update_product_stock(articul: int, data: models.StockUpdate, session: Session = Depends(database.get_session)):
    product = session.get(models.Product, articul)
    if not product:
        raise HTTPException(status_code=404, detail="Товар не найден")
    product.stock = data.stock
    session.add(product)
    session.commit()
    return {"message": "Остаток успешно обновлен", "stock": product.stock}

@app.get("/api/categories")
@app.get("/api/Products/categories")
def get_categories(session: Session = Depends(database.get_session)):
    categories = session.exec(select(models.Category)).all()
    return [{"id": c.id, "title": c.title} for c in categories]

@app.post("/api/categories")
def create_category(data: models.CategoryCreate, session: Session = Depends(database.get_session)):
    category = models.Category(title=data.title)
    session.add(category)
    session.commit()
    session.refresh(category)
    return {"id": category.id, "title": category.title}

@app.delete("/api/categories/{id}")
def delete_category(id: int, session: Session = Depends(database.get_session)):
    category = session.get(models.Category, id)
    if not category:
        raise HTTPException(status_code=404, detail="Категория не найдена")
    session.delete(category)
    session.commit()
    return {"message": "Категория успешно удалена"}

@app.get("/api/manufacturers")
def get_manufacturers(session: Session = Depends(database.get_session)):
    manufacturers = session.exec(select(models.Manufacturer)).all()
    return [{"id": m.id, "name": m.name} for m in manufacturers]

@app.post("/api/manufacturers")
def create_manufacturer(data: models.ManufacturerCreate, session: Session = Depends(database.get_session)):
    existing = session.exec(
        select(models.Manufacturer).where(col(models.Manufacturer.name).ilike(data.name.strip()))
    ).first()
    if existing:
        return {"id": existing.id, "name": existing.name}
    mfg = models.Manufacturer(name=data.name.strip())
    session.add(mfg)
    session.commit()
    session.refresh(mfg)
    return {"id": mfg.id, "name": mfg.name}

@app.get("/api/brands")
def get_brands(session: Session = Depends(database.get_session)):
    manufacturers = session.exec(select(models.Manufacturer.name)).all()
    return sorted(list({m for m in manufacturers if m}))

@app.get("/api/roles")
def get_roles(session: Session = Depends(database.get_session)):
    roles = session.exec(select(models.Role)).all()
    return [{"id": r.id, "name": r.name} for r in roles]

@app.get("/api/products/{articul}/variations")
@app.get("/api/Products/{articul}/variations")
def get_variations(articul: int, session: Session = Depends(database.get_session)):
    variations = session.exec(
        select(models.ProductVariation).where(models.ProductVariation.product_id == articul)
    ).all()
    return [
        {
            "id": v.id,
            "productId": v.product_id,
            "name": v.name,
            "price": v.price,
            "stock": v.stock
        } for v in variations
    ]

@app.post("/api/products/{articul}/variations")
def create_variation(articul: int, data: models.VariationCreate, session: Session = Depends(database.get_session)):
    variation = models.ProductVariation(
        product_id=articul,
        name=data.name,
        price=data.price,
        stock=data.stock
    )
    session.add(variation)
    session.commit()
    session.refresh(variation)
    return {
        "id": variation.id,
        "productId": variation.product_id,
        "name": variation.name,
        "price": variation.price,
        "stock": variation.stock
    }

@app.post("/api/products/variations")
@app.post("/api/Products/variations")
def create_variation_direct(data: models.VariationDirectCreate, session: Session = Depends(database.get_session)):
    variation = models.ProductVariation(
        product_id=data.productId,
        name=data.name,
        price=data.price,
        stock=data.stock
    )
    session.add(variation)
    session.commit()
    session.refresh(variation)
    return {
        "id": variation.id,
        "productId": variation.product_id,
        "name": variation.name,
        "price": variation.price,
        "stock": variation.stock
    }

@app.delete("/api/products/variations/{id}")
@app.delete("/api/Products/variations/{id}")
def delete_variation(id: int, session: Session = Depends(database.get_session)):
    variation = session.get(models.ProductVariation, id)
    if not variation:
        raise HTTPException(status_code=404, detail="Вариация не найдена")
    session.delete(variation)
    session.commit()
    return {"message": "Вариация удалена"}

@app.get("/api/products/{articul}/reviews")
def get_reviews(articul: int, session: Session = Depends(database.get_session)):
    reviews = session.exec(
        select(models.Review).where(models.Review.product_id == articul).order_by(col(models.Review.id).desc())
    ).all()
    res = []
    for r in reviews:
        user = session.get(models.User, r.user_id) if r.user_id else None
        name = user.name if user else "Покупатель"
        res.append({
            "id": r.id,
            "articul": r.product_id,
            "productId": r.product_id,
            "userId": r.user_id,
            "userName": name,
            "rating": r.rating,
            "date": r.date,
            "comment": r.comment
        })
    return res

@app.post("/api/products/{articul}/reviews")
def add_review(articul: int, data: models.ReviewCreate, session: Session = Depends(database.get_session)):
    product = session.get(models.Product, articul)
    if not product:
        raise HTTPException(status_code=404, detail="Товар не найден")

    user_name = (data.user_name or "").strip()
    user_id = data.user_id

    if user_id:
        user = session.get(models.User, user_id)
        if user and not user_name:
            user_name = user.name
    elif user_name:
        user = session.exec(select(models.User).where(models.User.name == user_name)).first()
        if user:
            user_id = user.user_id

    if not user_id:
        if user_name and user_name != "Покупатель":
            safe_email = f"user_{int(datetime.now().timestamp())}@shop.ru"
            user = models.User(name=user_name, phone="", email=safe_email, password="guest", role_id=1)
            session.add(user)
            session.commit()
            session.refresh(user)
            user_id = user.user_id
        else:
            first_user = session.exec(select(models.User)).first()
            user_id = first_user.user_id if first_user else 1
            if not user_name:
                user_name = first_user.name if first_user else "Покупатель"

    now_str = datetime.now().strftime("%d.%m.%Y")
    review = models.Review(
        product_id=articul,
        user_id=user_id,
        rating=data.rating,
        date=now_str,
        comment=data.comment
    )
    session.add(review)
    session.commit()
    session.refresh(review)

    return {
        "id": review.id,
        "articul": review.product_id,
        "productId": review.product_id,
        "userId": review.user_id,
        "userName": user_name,
        "rating": review.rating,
        "date": review.date,
        "comment": review.comment
    }

@app.post("/api/auth/register")
@app.post("/api/Users/register")
def register(data: models.RegisterRequest, session: Session = Depends(database.get_session)):
    if not data.password or " " in data.password or len(data.password) > 32:
        raise HTTPException(status_code=400, detail="Пароль не должен содержать пробелы и не может быть длиннее 32 символов")
    if not data.email or " " in data.email:
        raise HTTPException(status_code=400, detail="Email не должен содержать пробелы")

    existing = session.exec(
        select(models.User).where(
            (models.User.email == data.email) | (models.User.phone == data.phone)
        )
    ).first()
    if existing:
        raise HTTPException(status_code=400, detail="Пользователь с таким email или телефоном уже существует")

    role_id = data.role_id or get_or_create_role(data.role, session)

    user = models.User(
        role_id=role_id,
        name=data.name.strip(),
        phone=data.phone.strip(),
        email=data.email.strip(),
        password=data.password
    )
    session.add(user)
    session.commit()
    session.refresh(user)

    cart = models.Receipt(
        code="",
        user_id=user.user_id,
        total_price=0,
        date_time=datetime.now().isoformat(),
        status_id=1,
        order_status=0,
        address=""
    )
    session.add(cart)
    session.commit()

    return format_user_response(user, session)

@app.post("/api/auth/login")
@app.post("/api/Users/login")
async def login(
    request: Request,
    email: Optional[str] = None,
    password: Optional[str] = None,
    session: Session = Depends(database.get_session)
):
    req_email = email
    req_password = password

    if not req_email or not req_password:
        try:
            body = await request.json()
            if isinstance(body, dict):
                req_email = req_email or body.get("email")
                req_password = req_password or body.get("password")
        except Exception:
            pass

    if not req_email or not req_password:
        raise HTTPException(status_code=400, detail="Введите email и пароль")

    clean_phone = "".join(ch for ch in req_email if ch.isdigit())
    phone_filter = (models.User.phone == req_email)
    if clean_phone:
        phone_filter = phone_filter | (models.User.phone == clean_phone) | (models.User.phone == f"+{clean_phone}")

    user = session.exec(
        select(models.User).where(
            (models.User.email == req_email) | phone_filter,
            models.User.password == req_password
        )
    ).first()
    if not user:
        raise HTTPException(status_code=401, detail="Неверный email/телефон или пароль")

    return format_user_response(user, session)

@app.get("/api/users/{id}")
@app.get("/api/Users/{id}")
def get_user(id: int, session: Session = Depends(database.get_session)):
    user = session.get(models.User, id)
    if not user:
        raise HTTPException(status_code=404, detail="Пользователь не найден")
    return format_user_response(user, session)

@app.put("/api/users/{id}")
@app.put("/api/Users/{id}")
def update_user(id: int, data: models.UserUpdate, session: Session = Depends(database.get_session)):
    user = session.get(models.User, id)
    if not user:
        raise HTTPException(status_code=404, detail="Пользователь не найден")

    if data.password is not None:
        if " " in data.password or len(data.password) > 32 or len(data.password) < 4:
            raise HTTPException(status_code=400, detail="Пароль не должен содержать пробелы и должен быть от 4 до 32 символов")
        user.password = data.password

    if data.name is not None:
        user.name = data.name.strip()
    if data.phone is not None:
        user.phone = data.phone.strip()
    if data.email is not None:
        if " " in data.email:
            raise HTTPException(status_code=400, detail="Email не должен содержать пробелы")
        user.email = data.email.strip()
    if data.role_id is not None:
        user.role_id = data.role_id
    elif data.role is not None:
        user.role_id = get_or_create_role(data.role, session)

    session.add(user)
    session.commit()
    session.refresh(user)

    return format_user_response(user, session)

@app.get("/api/users")
@app.get("/api/Users")
def get_users(session: Session = Depends(database.get_session)):
    users = session.exec(select(models.User)).all()
    return [format_user_response(u, session) for u in users]

@app.get("/api/cart/{user_id}")
def get_cart(user_id: int, session: Session = Depends(database.get_session)):
    cart = session.exec(
        select(models.Receipt).where(
            models.Receipt.user_id == user_id,
            models.Receipt.order_status == 0
        )
    ).first()

    if not cart:
        cart = models.Receipt(
            code="",
            user_id=user_id,
            total_price=0,
            date_time=datetime.now().isoformat(),
            status_id=1,
            order_status=0,
            address=""
        )
        session.add(cart)
        session.commit()
        session.refresh(cart)

    return format_receipt(cart, session)

@app.post("/api/cart/add")
def add_to_cart(data: models.AddToCartRequest, session: Session = Depends(database.get_session)):
    product = session.get(models.Product, data.product_id)
    if not product:
        raise HTTPException(status_code=404, detail="Товар не найден")

    price = product.price
    if data.variation_id:
        var = session.get(models.ProductVariation, data.variation_id)
        if var:
            price = var.price

    receipt = session.get(models.Receipt, data.receipt_id)
    if not receipt:
        raise HTTPException(status_code=404, detail="Корзина не найдена")

    item = session.exec(
        select(models.ReceiptItem).where(
            models.ReceiptItem.receipt_id == data.receipt_id,
            models.ReceiptItem.product_id == data.product_id,
            models.ReceiptItem.variation_id == data.variation_id
        )
    ).first()

    if item:
        item.quantity += data.quantity
        item.price_at_purchase = price
        session.add(item)
    else:
        item = models.ReceiptItem(
            receipt_id=receipt.receipt_id or data.receipt_id or 0,
            product_id=data.product_id,
            variation_id=data.variation_id,
            quantity=data.quantity,
            price_at_purchase=price
        )
        session.add(item)

    session.commit()
    session.refresh(receipt)
    return {"message": "Товар добавлен в корзину"}

@app.put("/api/cart/update_quantity")
def update_cart_quantity(
    receipt_id: int,
    product_id: int,
    quantity: int,
    variation_id: Optional[int] = None,
    session: Session = Depends(database.get_session)
):
    if quantity <= 0:
        raise HTTPException(status_code=400, detail="Количество должно быть больше нуля")

    item = session.exec(
        select(models.ReceiptItem).where(
            models.ReceiptItem.receipt_id == receipt_id,
            models.ReceiptItem.product_id == product_id,
            models.ReceiptItem.variation_id == variation_id
        )
    ).first()

    if not item:
        raise HTTPException(status_code=404, detail="Товар в корзине не найден")

    item.quantity = quantity
    session.add(item)
    session.commit()
    return {"message": "Количество обновлено"}

@app.delete("/api/cart/delete_item/{receipt_id}/{product_id}")
def delete_cart_item(
    receipt_id: int,
    product_id: int,
    variation_id: Optional[int] = None,
    session: Session = Depends(database.get_session)
):
    item = session.exec(
        select(models.ReceiptItem).where(
            models.ReceiptItem.receipt_id == receipt_id,
            models.ReceiptItem.product_id == product_id,
            models.ReceiptItem.variation_id == variation_id
        )
    ).first()

    if not item:
        raise HTTPException(status_code=404, detail="Товар в корзине не найден")

    session.delete(item)
    session.commit()
    return {"message": "Товар удален из корзины"}

@app.post("/api/cart/checkout/{receipt_id}")
def checkout_cart(receipt_id: int, data: models.CheckoutRequest, session: Session = Depends(database.get_session)):
    cart = session.get(models.Receipt, receipt_id)
    if not cart:
        raise HTTPException(status_code=404, detail="Корзина не найдена")

    items = session.exec(
        select(models.ReceiptItem).where(models.ReceiptItem.receipt_id == receipt_id)
    ).all()

    if not items:
        raise HTTPException(status_code=400, detail="Корзина пуста")

    total = 0.0
    for item in items:
        prod = session.get(models.Product, item.product_id)
        item_price = prod.price if prod else item.price_at_purchase

        if item.variation_id:
            variation = session.get(models.ProductVariation, item.variation_id)
            if variation:
                item_price = variation.price
                variation.stock = max(0, variation.stock - item.quantity)
                session.add(variation)
        elif prod:
            prod.stock = max(0, prod.stock - item.quantity)
            session.add(prod)

        item.price_at_purchase = item_price
        total += item_price * item.quantity
        session.add(item)

    cart.total_price = total
    cart.order_status = 1
    cart.status_id = 2
    cart.date_time = datetime.now().strftime("%Y-%m-%dT%H:%M:%S")
    cart.address = data.address or ""
    cart.code = f"#{cart.receipt_id:05d}"

    session.add(cart)

    new_cart = models.Receipt(
        code="",
        user_id=cart.user_id,
        total_price=0,
        date_time=datetime.now().isoformat(),
        status_id=1,
        order_status=0,
        address=""
    )
    session.add(new_cart)

    session.commit()
    session.refresh(cart)
    return format_receipt(cart, session)

@app.get("/api/orders/user/{user_id}")
def get_user_orders(user_id: int, session: Session = Depends(database.get_session)):
    orders = session.exec(
        select(models.Receipt).where(
            models.Receipt.user_id == user_id,
            models.Receipt.order_status == 1
        ).order_by(col(models.Receipt.receipt_id).desc())
    ).all()
    return [format_receipt(o, session) for o in orders]

@app.get("/api/orders/{receipt_id}")
def get_order_by_id(receipt_id: int, session: Session = Depends(database.get_session)):
    order = session.get(models.Receipt, receipt_id)
    if not order:
        raise HTTPException(status_code=404, detail="Заказ не найден")
    return format_receipt(order, session)

@app.get("/api/orders")
@app.get("/api/Receipts/get_all_receipts")
def get_all_orders(session: Session = Depends(database.get_session)):
    orders = session.exec(
        select(models.Receipt).order_by(col(models.Receipt.receipt_id).desc())
    ).all()
    return [format_receipt(o, session) for o in orders]

@app.post("/api/orders")
@app.post("/api/orders/create")
@app.post("/api/Receipts/create")
def create_order(data: models.CreateOrderRequest, session: Session = Depends(database.get_session)):
    user = None
    if data.user_id:
        user = session.get(models.User, data.user_id)

    if not user and data.email:
        user = session.exec(select(models.User).where(models.User.email == data.email)).first()

    if not user and data.phone:
        user = session.exec(select(models.User).where(models.User.phone == data.phone)).first()

    if not user:
        email = data.email or f"customer_{int(datetime.now().timestamp())}@shop.ru"
        user = models.User(
            name=data.name or "Покупатель",
            phone=data.phone or "",
            email=email,
            password="guest",
            role_id=1
        )
        session.add(user)
        session.commit()
        session.refresh(user)

    now_str = datetime.now().strftime("%Y-%m-%dT%H:%M:%S")
    order = models.Receipt(
        code="",
        user_id=user.user_id,
        total_price=0.0,
        date_time=now_str,
        status_id=1,
        order_status=1,
        address=data.address or ""
    )
    session.add(order)
    session.commit()
    session.refresh(order)

    order.code = f"#TF-{datetime.now().year}-{order.receipt_id:05d}"

    total_price = 0.0
    for it in data.items:
        prod = session.get(models.Product, it.product_id)
        if not prod:
            continue

        item_price = prod.price
        if it.price is not None and it.price > 0:
            item_price = it.price

        if it.variation_id:
            variation = session.get(models.ProductVariation, it.variation_id)
            if variation:
                if it.price is None or it.price <= 0:
                    item_price = variation.price
                variation.stock = max(0, variation.stock - it.quantity)
                session.add(variation)
        else:
            prod.stock = max(0, prod.stock - it.quantity)
            session.add(prod)

        receipt_item = models.ReceiptItem(
            receipt_id=order.receipt_id or 0,
            product_id=it.product_id,
            variation_id=it.variation_id,
            quantity=it.quantity,
            price_at_purchase=item_price
        )
        session.add(receipt_item)
        total_price += item_price * it.quantity

    order.total_price = total_price
    session.add(order)
    session.commit()
    session.refresh(order)

    return format_receipt(order, session)

@app.put("/api/orders/{receipt_id}/status")
def update_order_status(receipt_id: int, data: models.StatusUpdate, session: Session = Depends(database.get_session)):
    order = session.get(models.Receipt, receipt_id)
    if not order:
        raise HTTPException(status_code=404, detail="Заказ не найден")

    status = session.get(models.Status, data.status_id)
    if not status or status.id is None:
        raise HTTPException(status_code=404, detail="Статус не найден")

    order.status_id = status.id
    session.add(order)
    session.commit()
    return {"message": "Статус заказа обновлен", "statusTitle": status.title}

@app.put("/api/Receipts/update_order_status/{receipt_id}")
def update_order_status_direct(
    receipt_id: int,
    status_id: int = Body(..., embed=False),
    session: Session = Depends(database.get_session)
):
    order = session.get(models.Receipt, receipt_id)
    if not order:
        raise HTTPException(status_code=404, detail="Заказ не найден")

    status = session.get(models.Status, status_id)
    order.status_id = status_id
    session.add(order)
    session.commit()
    return {"message": "Статус заказа успешно обновлен"}

@app.get("/api/statuses")
@app.get("/api/Receipts/statuses")
def get_statuses(session: Session = Depends(database.get_session)):
    statuses = session.exec(select(models.Status)).all()
    return [{"id": s.id, "title": s.title} for s in statuses]


class ChatMessage(BaseModel):
    role: str
    content: str

class ChatRequest(BaseModel):
    messages: List[ChatMessage]

@app.post("/api/chat")
async def chat_with_ai(request: ChatRequest):
    api_key = os.getenv("OPENAI_API_KEY")
    if not api_key or api_key == "dummy_key":
        database._load_env()
        api_key = os.getenv("OPENAI_API_KEY")

    if not api_key or api_key == "dummy_key":
        raise HTTPException(
            status_code=500,
            detail="Ключ OPENAI_API_KEY не задан в .env файле. Укажите действительный API-ключ."
        )

    try:
        base_url = os.getenv("OPENAI_BASE_URL") or None
        model_name = os.getenv("OPENAI_MODEL") or ("gemini-2.5-flash" if (base_url and "dogai" in base_url) else "gpt-4o-mini")
        client = AsyncOpenAI(api_key=api_key, base_url=base_url)
        system_prompt = {
            "role": "system",
            "content": (
                "Ты — вежливый и профессиональный эксперт-консультант в интернет-магазине электроники и техники. "
                "Твоя задача — помогать покупателям с выбором гаджетов, сравнивать их характеристики, "
                "подсказывать совместимость (например, материнская плата и процессор) и рекомендовать лучшие решения. "
                "Используй форматирование Markdown (таблицы, списки, выделения) для наглядности. "
                "Если пользователь задает вопросы не по теме электроники, вежливо возвращай разговор к товарам магазина."
            )
        }
        
        messages = [system_prompt] + [{"role": m.role, "content": m.content} for m in request.messages]

        response = await client.chat.completions.create(
            model=model_name,
            messages=messages,
            temperature=0.7,
            max_tokens=1000
        )
        
        reply = response.choices[0].message.content
        return {"reply": reply}

    except Exception as e:
        print(f"Ошибка чата OpenAI: {e}")
        raise HTTPException(status_code=500, detail=f"Ошибка AI-ассистента: {str(e)}")
