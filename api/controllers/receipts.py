import os
import sys
from datetime import datetime
from fastapi import APIRouter, Depends, HTTPException
from sqlmodel import Session, select

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
try:
    import database
    import models
except ImportError:
    from api import database, models

from .products import format_product
from .users import format_user_response

router = APIRouter(tags=["Receipts"])

def format_receipt(r: models.Receipt, session: Session):
    items = session.exec(
        select(models.ReceiptItem).where(models.ReceiptItem.receipt_id == r.receipt_id)
    ).all()

    formatted_items = []
    for item in items:
        var_data = None
        prod = None
        prod_id = None
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
                prod_id = v.product_id
                prod = session.get(models.Product, v.product_id)

        prod_data = format_product(prod, session) if prod else None

        formatted_items.append({
            "id": item.id,
            "receiptId": item.receipt_id,
            "productId": prod_id,
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

@router.get("/api/orders/user/{user_id}")
def get_user_orders(user_id: int, session: Session = Depends(database.get_session)):
    orders = session.exec(
        select(models.Receipt).where(
            models.Receipt.user_id == user_id,
            models.Receipt.order_status == 1
        ).order_by(models.Receipt.receipt_id.desc())
    ).all()
    return [format_receipt(o, session) for o in orders]

@router.get("/api/orders/{receipt_id}")
def get_order_by_id(receipt_id: int, session: Session = Depends(database.get_session)):
    order = session.get(models.Receipt, receipt_id)
    if not order:
        raise HTTPException(status_code=404, detail="Заказ не найден")
    return format_receipt(order, session)

@router.get("/api/orders")
def get_all_orders(session: Session = Depends(database.get_session)):
    orders = session.exec(
        select(models.Receipt).order_by(models.Receipt.receipt_id.desc())
    ).all()
    return [format_receipt(o, session) for o in orders]

@router.post("/api/orders")
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
        prod = None
        variation = None
        if it.variation_id:
            variation = session.get(models.ProductVariation, it.variation_id)
            if variation:
                prod = session.get(models.Product, variation.product_id)
        elif it.product_id:
            prod = session.get(models.Product, it.product_id)
            if prod:
                variation = session.exec(
                    select(models.ProductVariation).where(models.ProductVariation.product_id == prod.articul)
                ).first()
                if not variation:
                    variation = models.ProductVariation(
                        product_id=prod.articul,
                        name="Базовая",
                        price=prod.price,
                        stock=prod.stock
                    )
                    session.add(variation)
                    session.commit()
                    session.refresh(variation)

        if not prod and not variation:
            continue

        item_price = (it.price if it.price and it.price > 0 else None)
        if variation:
            if not item_price:
                item_price = variation.price
            variation.stock = max(0, variation.stock - it.quantity)
            session.add(variation)
        if prod:
            if not item_price:
                item_price = prod.price
            prod.stock = max(0, prod.stock - it.quantity)
            session.add(prod)

        receipt_item = models.ReceiptItem(
            receipt_id=order.receipt_id,
            variation_id=variation.id if variation else None,
            quantity=it.quantity,
            price_at_purchase=item_price or 0.0
        )
        session.add(receipt_item)
        total_price += (item_price or 0.0) * it.quantity

    order.total_price = total_price
    session.add(order)
    session.commit()
    session.refresh(order)

    return format_receipt(order, session)

@router.put("/api/orders/{receipt_id}/status")
def update_order_status(receipt_id: int, data: models.StatusUpdate, session: Session = Depends(database.get_session)):
    order = session.get(models.Receipt, receipt_id)
    if not order:
        raise HTTPException(status_code=404, detail="Заказ не найден")

    status = session.get(models.Status, data.status_id)
    if not status:
        raise HTTPException(status_code=404, detail="Статус не найден")

    order.status_id = status.id
    session.add(order)
    session.commit()
    return {"message": "Статус заказа обновлен", "statusTitle": status.title}


@router.delete("/api/receipts/clear_receipts/{user_id}")
def clear_user_receipts(user_id: int, session: Session = Depends(database.get_session)):
    receipts = session.exec(
        select(models.Receipt).where(models.Receipt.user_id == user_id)
    ).all()
    if not receipts:
        raise HTTPException(status_code=404, detail="Чеки пользователя не найдены")

    for r in receipts:
        items = session.exec(
            select(models.ReceiptItem).where(models.ReceiptItem.receipt_id == r.receipt_id)
        ).all()
        for it in items:
            session.delete(it)
        session.delete(r)

    session.commit()
    return {"message": "Все чеки успешно очищены"}
