import os
import sys
from typing import List
from contextlib import asynccontextmanager
from fastapi import FastAPI, HTTPException
from openai import AsyncOpenAI
from pydantic import BaseModel
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles

current_dir = os.path.dirname(os.path.abspath(__file__))
if current_dir not in sys.path:
    sys.path.insert(0, current_dir)

try:
    from . import database, models
except (ImportError, ValueError):
    import database
    import models

if "api.models" in sys.modules and "models" not in sys.modules:
    sys.modules["models"] = sys.modules["api.models"]
elif "models" in sys.modules and "api.models" not in sys.modules:
    sys.modules["api.models"] = sys.modules["models"]

if "api.database" in sys.modules and "database" not in sys.modules:
    sys.modules["database"] = sys.modules["api.database"]
elif "database" in sys.modules and "api.database" not in sys.modules:
    sys.modules["api.database"] = sys.modules["database"]

try:
    from .controllers import register_controllers
    from .controllers.products import format_product, get_or_create_manufacturer
    from .controllers.receipts import format_receipt
    from .controllers.users import format_user_response, get_or_create_role
except (ImportError, ValueError):
    from controllers import register_controllers
    from controllers.products import format_product, get_or_create_manufacturer
    from controllers.receipts import format_receipt
    from controllers.users import format_user_response, get_or_create_role

@asynccontextmanager
async def lifespan(app: FastAPI):
    database.init_db()
    yield

tags_metadata = [
    {"name": "Products"},
    {"name": "Categories"},
    {"name": "Manufacturers"},
    {"name": "Statuses"},
    {"name": "Reviews"},
    {"name": "Users"},
    {"name": "Roles"},
    {"name": "Cart"},
    {"name": "Receipts"},
]

app = FastAPI(
    title="Магазин техники API",
    openapi_tags=tags_metadata,
    lifespan=lifespan
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

images_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "images")
os.makedirs(images_dir, exist_ok=True)
app.mount("/images", StaticFiles(directory=images_dir), name="images")

register_controllers(app)


class ChatMessage(BaseModel):
    role: str
    content: str

class ChatRequest(BaseModel):
    messages: List[ChatMessage]

@app.post("/api/chat")
async def chat_with_ai(request: ChatRequest):
    api_key = os.getenv("OPENAI_API_KEY")
    if not api_key or api_key == "dummy_key":
        database._load_env()
        api_key = os.getenv("OPENAI_API_KEY")

    if not api_key or api_key == "dummy_key":
        raise HTTPException(
            status_code=500,
            detail="Ключ OPENAI_API_KEY не задан в .env файле. Укажите действительный API-ключ."
        )

    try:
        base_url = os.getenv("OPENAI_BASE_URL") or None
        model_name = os.getenv("OPENAI_MODEL") or ("gemini-2.5-flash" if (base_url and "dogai" in base_url) else "gpt-4o-mini")
        client = AsyncOpenAI(api_key=api_key, base_url=base_url)
        system_prompt = {
            "role": "system",
            "content": (
                "Ты — вежливый и профессиональный эксперт-консультант в интернет-магазине электроники и техники. "
                "Твоя задача — помогать покупателям с выбором гаджетов, сравнивать их характеристики, "
                "подсказывать совместимость (например, материнская плата и процессор) и рекомендовать лучшие решения. "
                "Используй форматирование Markdown (таблицы, списки, выделения) для наглядности. "
                "Если пользователь задает вопросы не по теме электроники, вежливо возвращай разговор к товарам магазина."
            )
        }

        messages = [system_prompt] + [{"role": m.role, "content": m.content} for m in request.messages]

        response = await client.chat.completions.create(
            model=model_name,
            messages=messages,
            temperature=0.7,
            max_tokens=1000
        )

        reply = response.choices[0].message.content
        return {"reply": reply}

    except Exception as e:
        print(f"Ошибка чата OpenAI: {e}")
        raise HTTPException(status_code=500, detail=f"Ошибка AI-ассистента: {str(e)}")
