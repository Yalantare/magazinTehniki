from typing import Optional
from sqlmodel import SQLModel, Field

class User(SQLModel, table=True):
    __tablename__ = "users"

    user_id: Optional[int] = Field(default=None, primary_key=True)
    role: str = Field(default="user")
    name: str
    phone: str
    password: str
    email: str = Field(unique=True, index=True)

class Category(SQLModel, table=True):
    __tablename__ = "categories"

    id: Optional[int] = Field(default=None, primary_key=True)
    title: str

class Product(SQLModel, table=True):
    __tablename__ = "products"

    articul: Optional[int] = Field(default=None, primary_key=True)
    title: str
    manufacturer: str
    category: int = Field(foreign_key="categories.id")
    price: float
    stock: int = Field(default=0)
    rating: float = Field(default=5.0)
    reviews_count: int = Field(default=0)
    description: str = Field(default="")
    photo: str = Field(default="")

class ProductVariation(SQLModel, table=True):
    __tablename__ = "product_variations"

    id: Optional[int] = Field(default=None, primary_key=True)
    product_id: int = Field(foreign_key="products.articul")
    name: str
    price: float
    stock: int = Field(default=0)

class Status(SQLModel, table=True):
    __tablename__ = "statuses"

    id: Optional[int] = Field(default=None, primary_key=True)
    title: str

class Receipt(SQLModel, table=True):
    __tablename__ = "receipts"

    receipt_id: Optional[int] = Field(default=None, primary_key=True)
    code: str = Field(default="")
    user_id: int = Field(foreign_key="users.user_id")
    total_price: float = Field(default=0.0)
    date_time: str = Field(default="")
    status: int = Field(default=1)
    status_title: str = Field(default="В обработке")
    order_status: int = Field(default=0)
    address: str = Field(default="")

class ReceiptItem(SQLModel, table=True):
    __tablename__ = "receipt_items"

    id: Optional[int] = Field(default=None, primary_key=True)
    receipt_id: int = Field(foreign_key="receipts.receipt_id")
    product_id: int = Field(foreign_key="products.articul")
    variation_id: Optional[int] = Field(default=None)
    quantity: int = Field(default=1)
    price_at_purchase: float = Field(default=0.0)

class Review(SQLModel, table=True):
    __tablename__ = "reviews"

    id: Optional[int] = Field(default=None, primary_key=True)
    articul: int = Field(foreign_key="products.articul")
    user_name: str
    rating: int = Field(default=5)
    date: str
    comment: str

class LoginRequest(SQLModel):
    email: str
    password: str

class RegisterRequest(SQLModel):
    name: str
    phone: str
    email: str
    password: str
    role: Optional[str] = "user"

class UserUpdate(SQLModel):
    name: Optional[str] = None
    phone: Optional[str] = None
    email: Optional[str] = None
    password: Optional[str] = None

class CategoryCreate(SQLModel):
    title: str

class VariationCreate(SQLModel):
    name: str
    price: float
    stock: int = 0

class ProductCreate(SQLModel):
    title: str
    manufacturer: str
    category: int
    price: float
    stock: int = 0
    description: Optional[str] = ""
    photo: Optional[str] = ""

class ProductUpdate(SQLModel):
    title: Optional[str] = None
    manufacturer: Optional[str] = None
    category: Optional[int] = None
    price: Optional[float] = None
    stock: Optional[int] = None
    description: Optional[str] = None
    photo: Optional[str] = None

class StockUpdate(SQLModel):
    stock: int

class AddToCartRequest(SQLModel):
    receipt_id: Optional[int] = None
    product_id: int
    quantity: int = 1
    variation_id: Optional[int] = None

class CheckoutRequest(SQLModel):
    address: Optional[str] = ""

class ReviewCreate(SQLModel):
    user_name: str
    rating: int = 5
    comment: str

class StatusUpdate(SQLModel):
    status_id: int

class VariationDirectCreate(SQLModel):
    productId: int
    name: str
    price: float
    stock: int = 0

class CreateOrderItemRequest(SQLModel):
    product_id: int
    variation_id: Optional[int] = None
    quantity: int = 1
    price: Optional[float] = None

class CreateOrderRequest(SQLModel):
    user_id: Optional[int] = None
    name: Optional[str] = None
    phone: Optional[str] = None
    email: Optional[str] = None
    address: Optional[str] = ""
    items: list[CreateOrderItemRequest] = []

