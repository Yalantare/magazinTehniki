import os
import sys
from datetime import datetime
from typing import Optional
from fastapi import APIRouter, Depends, HTTPException
from sqlmodel import Session, select

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
try:
    import database
    import models
except ImportError:
    from api import database, models

from .receipts import format_receipt

router = APIRouter(tags=["Cart"])

@router.get("/api/cart/{user_id}")
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

@router.post("/api/cart/add")
def add_to_cart(data: models.AddToCartRequest, session: Session = Depends(database.get_session)):
    var = None
    if data.variation_id:
        var = session.get(models.ProductVariation, data.variation_id)

    product = None
    if var:
        product = session.get(models.Product, var.product_id)
    elif data.product_id:
        product = session.get(models.Product, data.product_id)

    if not product and not var:
        raise HTTPException(status_code=404, detail="Товар не найден")

    if not var and product:
        var = session.exec(
            select(models.ProductVariation).where(models.ProductVariation.product_id == product.articul)
        ).first()
        if not var:
            var = models.ProductVariation(
                product_id=product.articul,
                name="Базовая",
                price=product.price,
                stock=product.stock
            )
            session.add(var)
            session.commit()
            session.refresh(var)

    price = var.price if var else (product.price if product else 0.0)

    receipt = session.get(models.Receipt, data.receipt_id)
    if not receipt:
        raise HTTPException(status_code=404, detail="Корзина не найдена")

    item = session.exec(
        select(models.ReceiptItem).where(
            models.ReceiptItem.receipt_id == data.receipt_id,
            models.ReceiptItem.variation_id == var.id
        )
    ).first()

    if item:
        item.quantity += data.quantity
        item.price_at_purchase = price
        session.add(item)
    else:
        item = models.ReceiptItem(
            receipt_id=data.receipt_id,
            variation_id=var.id,
            quantity=data.quantity,
            price_at_purchase=price
        )
        session.add(item)

    session.commit()
    session.refresh(receipt)
    return {"message": "Товар добавлен в корзину"}

@router.put("/api/cart/update_quantity")
def update_cart_quantity(
    receipt_id: int,
    product_id: Optional[int] = None,
    quantity: int = 1,
    variation_id: Optional[int] = None,
    session: Session = Depends(database.get_session)
):
    if quantity <= 0:
        raise HTTPException(status_code=400, detail="Количество должно быть больше нуля")

    item = None
    if variation_id:
        item = session.exec(
            select(models.ReceiptItem).where(
                models.ReceiptItem.receipt_id == receipt_id,
                models.ReceiptItem.variation_id == variation_id
            )
        ).first()

    if not item and product_id:
        items = session.exec(
            select(models.ReceiptItem).where(models.ReceiptItem.receipt_id == receipt_id)
        ).all()
        for it in items:
            if it.variation_id:
                v = session.get(models.ProductVariation, it.variation_id)
                if v and v.product_id == product_id:
                    item = it
                    break

    if not item:
        raise HTTPException(status_code=404, detail="Товар в корзине не найден")

    item.quantity = quantity
    session.add(item)
    session.commit()
    return {"message": "Количество обновлено"}

@router.delete("/api/cart/delete_item/{receipt_id}/{product_id}")
def delete_cart_item(
    receipt_id: int,
    product_id: int,
    variation_id: Optional[int] = None,
    session: Session = Depends(database.get_session)
):
    item = None
    if variation_id:
        item = session.exec(
            select(models.ReceiptItem).where(
                models.ReceiptItem.receipt_id == receipt_id,
                models.ReceiptItem.variation_id == variation_id
            )
        ).first()

    if not item and product_id:
        items = session.exec(
            select(models.ReceiptItem).where(models.ReceiptItem.receipt_id == receipt_id)
        ).all()
        for it in items:
            if it.variation_id:
                v = session.get(models.ProductVariation, it.variation_id)
                if v and v.product_id == product_id:
                    item = it
                    break

    if not item:
        raise HTTPException(status_code=404, detail="Товар в корзине не найден")

    session.delete(item)
    session.commit()
    return {"message": "Товар удален из корзины"}

@router.post("/api/cart/checkout/{receipt_id}")
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
        item_price = item.price_at_purchase
        if item.variation_id:
            variation = session.get(models.ProductVariation, item.variation_id)
            if variation:
                item_price = variation.price
                variation.stock = max(0, variation.stock - item.quantity)
                session.add(variation)
                prod = session.get(models.Product, variation.product_id)
                if prod:
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
