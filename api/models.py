from typing import Optional
from sqlmodel import SQLModel, Field

class Role(SQLModel, table=True):
    __tablename__ = "roles"

    id: Optional[int] = Field(default=None, primary_key=True)
    name: str = Field(unique=True, index=True)

class Category(SQLModel, table=True):
    __tablename__ = "categories"

    id: Optional[int] = Field(default=None, primary_key=True)
    title: str = Field(unique=True, index=True)

class Manufacturer(SQLModel, table=True):
    __tablename__ = "manufacturers"

    id: Optional[int] = Field(default=None, primary_key=True)
    name: str = Field(unique=True, index=True)

class Status(SQLModel, table=True):
    __tablename__ = "statuses"

    id: Optional[int] = Field(default=None, primary_key=True)
    title: str = Field(unique=True)

class User(SQLModel, table=True):
    __tablename__ = "users"

    user_id: Optional[int] = Field(default=None, primary_key=True)
    role_id: int = Field(default=1, foreign_key="roles.id")
    name: str
    phone: str = Field(index=True)
    password: str
    email: str = Field(unique=True, index=True)

class Product(SQLModel, table=True):
    __tablename__ = "products"

    articul: Optional[int] = Field(default=None, primary_key=True)
    title: str = Field(index=True)
    manufacturer_id: int = Field(foreign_key="manufacturers.id")
    category_id: int = Field(foreign_key="categories.id")
    price: float
    stock: int = Field(default=0)
    description: str = Field(default="")
    photo: str = Field(default="")

    @property
    def category(self) -> int:
        return self.category_id

    @category.setter
    def category(self, val: int):
        self.category_id = val

class ProductVariation(SQLModel, table=True):
    __tablename__ = "product_variations"

    id: Optional[int] = Field(default=None, primary_key=True)
    product_id: int = Field(foreign_key="products.articul")
    name: str
    price: float
    stock: int = Field(default=0)

class Receipt(SQLModel, table=True):
    __tablename__ = "receipts"

    receipt_id: Optional[int] = Field(default=None, primary_key=True)
    code: str = Field(default="")
    user_id: int = Field(foreign_key="users.user_id")
    total_price: float = Field(default=0.0)
    date_time: str = Field(default="")
    status_id: int = Field(default=1, foreign_key="statuses.id")
    order_status: int = Field(default=0)
    address: str = Field(default="")

    @property
    def status(self) -> int:
        return self.status_id

    @status.setter
    def status(self, val: int):
        self.status_id = val

class ReceiptItem(SQLModel, table=True):
    __tablename__ = "receipt_items"

    id: Optional[int] = Field(default=None, primary_key=True)
    receipt_id: int = Field(foreign_key="receipts.receipt_id")
    product_id: int = Field(foreign_key="products.articul")
    variation_id: Optional[int] = Field(default=None, foreign_key="product_variations.id")
    quantity: int = Field(default=1)
    price_at_purchase: float = Field(default=0.0)

class Review(SQLModel, table=True):
    __tablename__ = "reviews"

    id: Optional[int] = Field(default=None, primary_key=True)
    articul: int = Field(foreign_key="products.articul")
    user_id: Optional[int] = Field(default=None, foreign_key="users.user_id")
    user_name: Optional[str] = Field(default="")
    rating: int = Field(default=5)
    date: str = Field(default="")
    comment: str = Field(default="")

class LoginRequest(SQLModel):
    email: str
    password: str

class RegisterRequest(SQLModel):
    name: str
    phone: str
    email: str
    password: str
    role: Optional[str] = "user"
    role_id: Optional[int] = None

class UserUpdate(SQLModel):
    name: Optional[str] = None
    phone: Optional[str] = None
    email: Optional[str] = None
    password: Optional[str] = None
    role: Optional[str] = None
    role_id: Optional[int] = None

class CategoryCreate(SQLModel):
    title: str

class ManufacturerCreate(SQLModel):
    name: str

class VariationCreate(SQLModel):
    name: str
    price: float
    stock: int = 0

class VariationDirectCreate(SQLModel):
    productId: int
    name: str
    price: float
    stock: int = 0

class ProductCreate(SQLModel):
    title: str
    manufacturer: Optional[str] = None
    manufacturer_id: Optional[int] = None
    category: Optional[int] = None
    category_id: Optional[int] = None
    price: float
    stock: int = 0
    description: Optional[str] = ""
    photo: Optional[str] = ""

class ProductUpdate(SQLModel):
    title: Optional[str] = None
    manufacturer: Optional[str] = None
    manufacturer_id: Optional[int] = None
    category: Optional[int] = None
    category_id: Optional[int] = None
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
    user_id: Optional[int] = None
    user_name: Optional[str] = ""
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

