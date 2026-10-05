import os
import sys
from pydantic import BaseModel
from fastapi import APIRouter, Depends, HTTPException
from sqlmodel import Session, select

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
try:
    import database
    import models
except ImportError:
    from api import database, models

router = APIRouter(tags=["Roles"])

class RoleCreate(BaseModel):
    name: str

@router.get("/api/roles")
def get_roles(session: Session = Depends(database.get_session)):
    roles = session.exec(select(models.Role)).all()
    return [{"id": r.id, "name": r.name} for r in roles]

@router.get("/api/roles/{id}")
def get_role(id: int, session: Session = Depends(database.get_session)):
    role = session.get(models.Role, id)
    if not role:
        raise HTTPException(status_code=404, detail="Роль не найдена")
    return {"id": role.id, "name": role.name}

@router.post("/api/roles")
def create_role(data: RoleCreate, session: Session = Depends(database.get_session)):
    role_name = data.name.strip().lower()
    existing = session.exec(select(models.Role).where(models.Role.name == role_name)).first()
    if existing:
        return {"id": existing.id, "name": existing.name}
    role = models.Role(name=role_name)
    session.add(role)
    session.commit()
    session.refresh(role)
    return {"id": role.id, "name": role.name}

@router.delete("/api/roles/{id}")
def delete_role(id: int, session: Session = Depends(database.get_session)):
    role = session.get(models.Role, id)
    if not role:
        raise HTTPException(status_code=404, detail="Роль не найдена")
    session.delete(role)
    session.commit()
    return {"message": "Роль успешно удалена"}
