import os
import sys
from typing import Optional
from fastapi import APIRouter, Depends, HTTPException, Response
from sqlmodel import Session, select, col, or_

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
try:
    import database
    import models
except ImportError:
    from api import database, models

router = APIRouter(tags=["Products"])


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


def sync_product_stock(product: models.Product, session: Session, commit: bool = True) -> int:
    """Синхронизирует products.stock с суммой остатков его вариаций (если вариации есть)."""
    if product.articul is None:
        return max(0, int(product.stock or 0))

    variations = session.exec(
        select(models.ProductVariation).where(models.ProductVariation.product_id == product.articul)
    ).all()

    if variations:
        effective_stock = sum(max(0, int(v.stock or 0)) for v in variations)
    else:
        effective_stock = max(0, int(product.stock or 0))

    if product.stock != effective_stock:
        product.stock = effective_stock
        session.add(product)
        if commit:
            session.commit()
            session.refresh(product)

    return effective_stock


def format_product(p: models.Product, session: Session):
    cat = session.get(models.Category, p.category_id) if p.category_id else None
    mfg = session.get(models.Manufacturer, p.manufacturer_id) if p.manufacturer_id else None
    variations = session.exec(
        select(models.ProductVariation).where(models.ProductVariation.product_id == p.articul)
    ).all()

    # Единый источник истины по остаткам: при наличии вариаций общий остаток всегда равен их сумме
    if variations:
        effective_stock = sum(max(0, int(v.stock or 0)) for v in variations)
        if p.stock != effective_stock:
            p.stock = effective_stock
            session.add(p)
            session.commit()
            session.refresh(p)
    else:
        effective_stock = max(0, int(p.stock or 0))

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
        "stock": effective_stock,
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
                "stock": max(0, int(v.stock or 0))
            } for v in variations
        ]
    }


@router.get("/api/products")
def get_products(
    response: Response,
    categoryId: Optional[int] = None,
    manufacturerId: Optional[int] = None,
    brand: Optional[str] = None,
    search: Optional[str] = None,
    minPrice: Optional[float] = None,
    maxPrice: Optional[float] = None,
    inStock: Optional[bool] = None,
    session: Session = Depends(database.get_session)
):
    response.headers["Cache-Control"] = "no-store, no-cache, must-revalidate"
    stmt = select(models.Product)
    joined_mfg = False

    if categoryId is not None:
        stmt = stmt.where(models.Product.category_id == categoryId)
    if manufacturerId is not None:
        stmt = stmt.where(models.Product.manufacturer_id == manufacturerId)
    if brand:
        stmt = stmt.join(
            models.Manufacturer,
            col(models.Product.manufacturer_id) == models.Manufacturer.id,
            isouter=True
        ).where(col(models.Manufacturer.name).ilike(f"%{brand.strip()}%"))
        joined_mfg = True
    if search:
        q = f"%{search.strip()}%"
        if not joined_mfg:
            stmt = stmt.join(
                models.Manufacturer,
                col(models.Product.manufacturer_id) == models.Manufacturer.id,
                isouter=True
            )
            joined_mfg = True
        stmt = stmt.where(
            or_(
                col(models.Product.title).ilike(q),
                col(models.Product.description).ilike(q),
                col(models.Manufacturer.name).ilike(q)
            )
        )
    if minPrice is not None:
        stmt = stmt.where(models.Product.price >= minPrice)
    if maxPrice is not None:
        stmt = stmt.where(models.Product.price <= maxPrice)

    products = session.exec(stmt).all()
    formatted = [format_product(p, session) for p in products]

    if inStock is True:
        formatted = [p for p in formatted if p["stock"] > 0]
    elif inStock is False:
        formatted = [p for p in formatted if p["stock"] <= 0]

    return formatted


@router.get("/api/products/{articul}")
def get_product(articul: int, response: Response, session: Session = Depends(database.get_session)):
    response.headers["Cache-Control"] = "no-store, no-cache, must-revalidate"
    product = session.get(models.Product, articul)
    if not product:
        raise HTTPException(status_code=404, detail="Товар не найден")
    return format_product(product, session)


@router.post("/api/products")
def create_product(data: models.ProductCreate, session: Session = Depends(database.get_session)):
    existing = session.exec(
        select(models.Product).where(col(models.Product.title).ilike(data.title.strip()))
    ).first()
    if existing:
        raise HTTPException(status_code=400, detail="Товар с таким названием уже существует")

    mfg_id = data.manufacturer_id
    if not mfg_id and data.manufacturer:
        mfg_id = get_or_create_manufacturer(data.manufacturer, session)
    if not mfg_id:
        mfg_id = get_or_create_manufacturer("Unknown", session)

    cat_id = data.category_id or data.category or 1

    product = models.Product(
        title=data.title.strip(),
        manufacturer_id=mfg_id,
        category_id=cat_id,
        price=data.price,
        stock=max(0, int(data.stock or 0)),
        description=data.description or "",
        photo=data.photo or ""
    )
    session.add(product)
    session.commit()
    session.refresh(product)
    return format_product(product, session)


@router.put("/api/products/{articul}")
def update_product(articul: int, data: models.ProductUpdate, session: Session = Depends(database.get_session)):
    product = session.get(models.Product, articul)
    if not product:
        raise HTTPException(status_code=404, detail="Товар не найден")

    if data.title is not None and data.title.strip().lower() != product.title.strip().lower():
        existing = session.exec(
            select(models.Product).where(
                col(models.Product.title).ilike(data.title.strip()),
                models.Product.articul != articul
            )
        ).first()
        if existing:
            raise HTTPException(status_code=400, detail="Товар с таким названием уже существует")

    if data.title is not None:
        product.title = data.title.strip()
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
    if data.description is not None:
        product.description = data.description
    if data.photo is not None:
        product.photo = data.photo

    variations = session.exec(
        select(models.ProductVariation).where(models.ProductVariation.product_id == articul)
    ).all()

    if data.stock is not None:
        new_stock = max(0, int(data.stock))
        if len(variations) == 0:
            product.stock = new_stock
        elif len(variations) == 1:
            # Единственная вариация (например, «Базовая» или одна модификация):
            # синхронизируем её остаток (и цену для «Базовая»), чтобы не было расхождений
            variations[0].stock = new_stock
            if variations[0].name.strip().lower() == "базовая" and data.price is not None:
                variations[0].price = data.price
            session.add(variations[0])
            product.stock = new_stock
        else:
            # Несколько вариаций: если явно обнулили товар — обнуляем все вариации,
            # иначе оставляем сумму остатков вариаций как истину
            if new_stock == 0:
                for v in variations:
                    v.stock = 0
                    session.add(v)
                product.stock = 0
            else:
                product.stock = sum(max(0, int(v.stock or 0)) for v in variations)
    elif variations:
        product.stock = sum(max(0, int(v.stock or 0)) for v in variations)

    session.add(product)
    session.commit()
    session.refresh(product)
    return format_product(product, session)


@router.delete("/api/products/{articul}")
def delete_product(articul: int, session: Session = Depends(database.get_session)):
    product = session.get(models.Product, articul)
    if not product:
        raise HTTPException(status_code=404, detail="Товар не найден")

    # Удаляем связанные отзывы
    reviews = session.exec(select(models.Review).where(models.Review.product_id == articul)).all()
    for r in reviews:
        session.delete(r)

    # Отвязываем позиции чеков от вариаций и удаляем вариации
    variations = session.exec(
        select(models.ProductVariation).where(models.ProductVariation.product_id == articul)
    ).all()
    for v in variations:
        if v.id is not None:
            items = session.exec(
                select(models.ReceiptItem).where(models.ReceiptItem.variation_id == v.id)
            ).all()
            for it in items:
                it.variation_id = None
                session.add(it)
        session.delete(v)

    session.delete(product)
    session.commit()
    return {"message": "Товар успешно удален"}


@router.put("/api/products/{articul}/stock")
def update_product_stock(articul: int, data: models.StockUpdate, session: Session = Depends(database.get_session)):
    product = session.get(models.Product, articul)
    if not product:
        raise HTTPException(status_code=404, detail="Товар не найден")

    new_stock = max(0, int(data.stock))
    variations = session.exec(
        select(models.ProductVariation).where(models.ProductVariation.product_id == articul)
    ).all()

    if len(variations) == 1:
        variations[0].stock = new_stock
        session.add(variations[0])
        product.stock = new_stock
    elif len(variations) > 1:
        if new_stock == 0:
            for v in variations:
                v.stock = 0
                session.add(v)
            product.stock = 0
        else:
            product.stock = sum(max(0, int(v.stock or 0)) for v in variations)
    else:
        product.stock = new_stock

    session.add(product)
    session.commit()
    return {"message": "Остаток успешно обновлен", "stock": product.stock}


@router.get("/api/products/{articul}/variations")
def get_variations(articul: int, response: Response, session: Session = Depends(database.get_session)):
    response.headers["Cache-Control"] = "no-store, no-cache, must-revalidate"
    variations = session.exec(
        select(models.ProductVariation).where(models.ProductVariation.product_id == articul)
    ).all()
    return [
        {
            "id": v.id,
            "productId": v.product_id,
            "name": v.name,
            "price": v.price,
            "stock": max(0, int(v.stock or 0))
        } for v in variations
    ]


@router.post("/api/products/{articul}/variations")
def create_variation(articul: int, data: models.VariationCreate, session: Session = Depends(database.get_session)):
    product = session.get(models.Product, articul)
    if not product:
        raise HTTPException(status_code=404, detail="Товар не найден")

    variation = models.ProductVariation(
        product_id=articul,
        name=data.name.strip(),
        price=data.price,
        stock=max(0, int(data.stock or 0))
    )
    session.add(variation)
    session.commit()
    session.refresh(variation)

    sync_product_stock(product, session, commit=True)

    return {
        "id": variation.id,
        "productId": variation.product_id,
        "name": variation.name,
        "price": variation.price,
        "stock": variation.stock,
        "productStock": product.stock
    }


@router.put("/api/products/variations/{id}")
def update_variation(id: int, data: models.VariationCreate, session: Session = Depends(database.get_session)):
    variation = session.get(models.ProductVariation, id)
    if not variation:
        raise HTTPException(status_code=404, detail="Вариация не найдена")

    variation.name = data.name.strip()
    variation.price = data.price
    variation.stock = max(0, int(data.stock or 0))
    session.add(variation)
    session.commit()
    session.refresh(variation)

    product = session.get(models.Product, variation.product_id)
    if product:
        sync_product_stock(product, session, commit=True)

    return {
        "id": variation.id,
        "productId": variation.product_id,
        "name": variation.name,
        "price": variation.price,
        "stock": variation.stock,
        "productStock": product.stock if product else variation.stock
    }


@router.delete("/api/products/variations/{id}")
def delete_variation(id: int, session: Session = Depends(database.get_session)):
    variation = session.get(models.ProductVariation, id)
    if not variation:
        raise HTTPException(status_code=404, detail="Вариация не найдена")

    product_id = variation.product_id

    # Отвязываем исторические чеки от удаляемой вариации, чтобы не падал FK
    items = session.exec(
        select(models.ReceiptItem).where(models.ReceiptItem.variation_id == id)
    ).all()
    for it in items:
        it.variation_id = None
        session.add(it)

    session.delete(variation)
    session.commit()

    product = session.get(models.Product, product_id)
    if product:
        remaining = session.exec(
            select(models.ProductVariation).where(models.ProductVariation.product_id == product_id)
        ).all()
        if remaining:
            product.stock = sum(max(0, int(v.stock or 0)) for v in remaining)
            session.add(product)
            session.commit()

    return {"message": "Вариация удалена", "productStock": product.stock if product else 0}
