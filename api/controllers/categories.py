import os
import sys
from fastapi import APIRouter, Depends, HTTPException
from sqlmodel import Session, select

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
try:
    import database
    import models
except ImportError:
    from api import database, models

router = APIRouter(tags=["Categories"])

@router.get("/api/categories")
def get_categories(session: Session = Depends(database.get_session)):
    categories = session.exec(select(models.Category)).all()
    return [{"id": c.id, "title": c.title} for c in categories]

@router.get("/api/categories/{id}")
def get_category(id: int, session: Session = Depends(database.get_session)):
    category = session.get(models.Category, id)
    if not category:
        raise HTTPException(status_code=404, detail="Категория не найдена")
    return {"id": category.id, "title": category.title}

@router.post("/api/categories")
def create_category(data: models.CategoryCreate, session: Session = Depends(database.get_session)):
    existing = session.exec(select(models.Category).where(models.Category.title == data.title.strip())).first()
    if existing:
        return {"id": existing.id, "title": existing.title}
    category = models.Category(title=data.title.strip())
    session.add(category)
    session.commit()
    session.refresh(category)
    return {"id": category.id, "title": category.title}

@router.put("/api/categories/{id}")
def update_category(id: int, data: models.CategoryCreate, session: Session = Depends(database.get_session)):
    category = session.get(models.Category, id)
    if not category:
        raise HTTPException(status_code=404, detail="Категория не найдена")
    category.title = data.title.strip()
    session.add(category)
    session.commit()
    session.refresh(category)
    return {"id": category.id, "title": category.title}

@router.delete("/api/categories/{id}")
def delete_category(id: int, session: Session = Depends(database.get_session)):
    category = session.get(models.Category, id)
    if not category:
        raise HTTPException(status_code=404, detail="Категория не найдена")
    session.delete(category)
    session.commit()
    return {"message": "Категория успешно удалена"}
