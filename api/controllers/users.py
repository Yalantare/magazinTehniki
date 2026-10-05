import os
import sys
from datetime import datetime
from typing import Optional
from fastapi import APIRouter, Depends, HTTPException, Request
from sqlmodel import Session, select

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
try:
    import database
    import models
except ImportError:
    from api import database, models

router = APIRouter(tags=["Users"])

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

@router.post("/api/users/register")
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


@router.post("/api/users/login")
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


@router.get("/api/users/{id}")
def get_user(id: int, session: Session = Depends(database.get_session)):
    user = session.get(models.User, id)
    if not user:
        raise HTTPException(status_code=404, detail="Пользователь не найден")
    return format_user_response(user, session)

@router.put("/api/users/{id}")
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


@router.delete("/api/users/{id}")
def delete_user(id: int, session: Session = Depends(database.get_session)):
    user = session.get(models.User, id)
    if not user:
        raise HTTPException(status_code=404, detail="Пользователь не найден")
    session.delete(user)
    session.commit()
    return {"message": "Пользователь успешно удален"}

@router.get("/api/users")
def get_users(session: Session = Depends(database.get_session)):
    users = session.exec(select(models.User)).all()
    return [format_user_response(u, session) for u in users]
