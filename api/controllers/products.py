import os
import sys
from typing import Optional
from fastapi import APIRouter, Depends, HTTPException
from sqlmodel import Session, select, col

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


@router.get("/api/products")
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


@router.get("/api/products/{articul}")
def get_product(articul: int, session: Session = Depends(database.get_session)):
    product = session.get(models.Product, articul)
    if not product:
        raise HTTPException(status_code=404, detail="Товар не найден")
    return format_product(product, session)

@router.post("/api/products")
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

@router.put("/api/products/{articul}")
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

@router.delete("/api/products/{articul}")
def delete_product(articul: int, session: Session = Depends(database.get_session)):
    product = session.get(models.Product, articul)
    if not product:
        raise HTTPException(status_code=404, detail="Товар не найден")
    session.delete(product)
    session.commit()
    return {"message": "Товар успешно удален"}

@router.put("/api/products/{articul}/stock")
def update_product_stock(articul: int, data: models.StockUpdate, session: Session = Depends(database.get_session)):
    product = session.get(models.Product, articul)
    if not product:
        raise HTTPException(status_code=404, detail="Товар не найден")
    product.stock = data.stock
    session.add(product)
    session.commit()
    return {"message": "Остаток успешно обновлен", "stock": product.stock}

@router.get("/api/products/{articul}/variations")
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

@router.post("/api/products/{articul}/variations")
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


@router.delete("/api/products/variations/{id}")
def delete_variation(id: int, session: Session = Depends(database.get_session)):
    variation = session.get(models.ProductVariation, id)
    if not variation:
        raise HTTPException(status_code=404, detail="Вариация не найдена")
    session.delete(variation)
    session.commit()
    return {"message": "Вариация удалена"}
