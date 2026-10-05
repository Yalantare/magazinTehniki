import os
import sys
from typing import Optional
from pydantic import BaseModel
from fastapi import APIRouter, Depends, HTTPException
from sqlmodel import Session, select

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
try:
    import database
    import models
except ImportError:
    from api import database, models

router = APIRouter(tags=["Statuses"])

class StatusCreate(BaseModel):
    title: str

@router.get("/api/statuses")
def get_statuses(session: Session = Depends(database.get_session)):
    statuses = session.exec(select(models.Status)).all()
    return [{"id": s.id, "title": s.title} for s in statuses]

@router.get("/api/statuses/{id}")
def get_status(id: int, session: Session = Depends(database.get_session)):
    status = session.get(models.Status, id)
    if not status:
        raise HTTPException(status_code=404, detail="Статус не найден")
    return {"id": status.id, "title": status.title}

@router.post("/api/statuses")
def create_status(data: StatusCreate, session: Session = Depends(database.get_session)):
    existing = session.exec(select(models.Status).where(models.Status.title == data.title.strip())).first()
    if existing:
        return {"id": existing.id, "title": existing.title}
    status = models.Status(title=data.title.strip())
    session.add(status)
    session.commit()
    session.refresh(status)
    return {"id": status.id, "title": status.title}

@router.delete("/api/statuses/{id}")
def delete_status(id: int, session: Session = Depends(database.get_session)):
    status = session.get(models.Status, id)
    if not status:
        raise HTTPException(status_code=404, detail="Статус не найден")
    session.delete(status)
    session.commit()
    return {"message": "Статус успешно удален"}
