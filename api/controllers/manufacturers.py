import os
import sys
from fastapi import APIRouter, Depends, HTTPException
from sqlmodel import Session, select, col

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
try:
    import database
    import models
except ImportError:
    from api import database, models

router = APIRouter(tags=["Manufacturers"])

@router.get("/api/manufacturers")
def get_manufacturers(session: Session = Depends(database.get_session)):
    manufacturers = session.exec(select(models.Manufacturer)).all()
    return [{"id": m.id, "name": m.name} for m in manufacturers]

@router.get("/api/brands")
def get_brands(session: Session = Depends(database.get_session)):
    manufacturers = session.exec(select(models.Manufacturer.name)).all()
    return sorted(list({m for m in manufacturers if m}))

@router.get("/api/manufacturers/{id}")
def get_manufacturer(id: int, session: Session = Depends(database.get_session)):
    mfg = session.get(models.Manufacturer, id)
    if not mfg:
        raise HTTPException(status_code=404, detail="Производитель не найден")
    return {"id": mfg.id, "name": mfg.name}

@router.post("/api/manufacturers")
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


@router.put("/api/manufacturers/{id}")
def update_manufacturer(id: int, data: models.ManufacturerCreate, session: Session = Depends(database.get_session)):
    mfg = session.get(models.Manufacturer, id)
    if not mfg:
        raise HTTPException(status_code=404, detail="Производитель не найден")
    mfg.name = data.name.strip()
    session.add(mfg)
    session.commit()
    session.refresh(mfg)
    return {"id": mfg.id, "name": mfg.name}

@router.delete("/api/manufacturers/{id}")
def delete_manufacturer(id: int, session: Session = Depends(database.get_session)):
    mfg = session.get(models.Manufacturer, id)
    if not mfg:
        raise HTTPException(status_code=404, detail="Производитель не найден")
    session.delete(mfg)
    session.commit()
    return {"message": "Производитель успешно удален"}
