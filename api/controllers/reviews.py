import os
import sys
from datetime import datetime
from fastapi import APIRouter, Depends, HTTPException
from sqlmodel import Session, select, col

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
try:
    import database
    import models
except ImportError:
    from api import database, models

router = APIRouter(tags=["Reviews"])

@router.get("/api/products/{articul}/reviews")
def get_product_reviews(articul: int, session: Session = Depends(database.get_session)):
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


@router.post("/api/products/{articul}/reviews")
def add_product_review(articul: int, data: models.ReviewCreate, session: Session = Depends(database.get_session)):
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


@router.get("/api/reviews")
def get_all_reviews(session: Session = Depends(database.get_session)):
    reviews = session.exec(select(models.Review).order_by(models.Review.id.desc())).all()
    res = []
    for r in reviews:
        name = r.user_name
        if not name and r.user_id:
            u = session.get(models.User, r.user_id)
            if u:
                name = u.name
        res.append({
            "id": r.id,
            "articul": r.articul,
            "userId": r.user_id,
            "userName": name or "Покупатель",
            "rating": r.rating,
            "date": r.date,
            "comment": r.comment
        })
    return res

@router.delete("/api/reviews/{id}")
def delete_review(id: int, session: Session = Depends(database.get_session)):
    review = session.get(models.Review, id)
    if not review:
        raise HTTPException(status_code=404, detail="Отзыв не найден")
    session.delete(review)
    session.commit()
    return {"message": "Отзыв успешно удален"}
