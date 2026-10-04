from typing import Optional
from sqlmodel import SQLModel, Field

class Role(SQLModel, table=True):
    __tablename__ = "roles"
    __table_args__ = {"extend_existing": True}

    id: Optional[int] = Field(default=None, primary_key=True)
    name: str = Field(unique=True, index=True)

class Category(SQLModel, table=True):
    __tablename__ = "categories"
    __table_args__ = {"extend_existing": True}

    id: Optional[int] = Field(default=None, primary_key=True)
    title: str = Field(unique=True, index=True)

class Manufacturer(SQLModel, table=True):
    __tablename__ = "manufacturers"
    __table_args__ = {"extend_existing": True}

    id: Optional[int] = Field(default=None, primary_key=True)
    name: str = Field(unique=True, index=True)

class Status(SQLModel, table=True):
    __tablename__ = "statuses"
    __table_args__ = {"extend_existing": True}

    id: Optional[int] = Field(default=None, primary_key=True)
    title: str = Field(unique=True)

class User(SQLModel, table=True):
    __tablename__ = "users"
    __table_args__ = {"extend_existing": True}

    user_id: Optional[int] = Field(default=None, primary_key=True)
    role_id: Optional[int] = Field(default=1, foreign_key="roles.id")
    name: str
    phone: str = Field(index=True)
    password: str
    email: str = Field(unique=True, index=True)

    @property
    def role(self) -> str:
        return "admin" if self.role_id == 2 else "user"

    @role.setter
    def role(self, val: str):
        if str(val).lower() == "admin":
            self.role_id = 2
        else:
            self.role_id = 1

    def __init__(self, **data):
        if "role" in data and "role_id" not in data:
            val = data.pop("role")
            data["role_id"] = 2 if str(val).lower() == "admin" else 1
        elif "role" in data:
            data.pop("role")
        super().__init__(**data)

class Product(SQLModel, table=True):
    __tablename__ = "products"
    __table_args__ = {"extend_existing": True}

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

    def __init__(self, **data):
        if "rating" in data:
            data.pop("rating")
        if "reviews_count" in data:
            data.pop("reviews_count")
        if "manufacturer" in data and "manufacturer_id" not in data:
            mfg = data.pop("manufacturer")
            if isinstance(mfg, int):
                data["manufacturer_id"] = mfg
        if "category" in data and "category_id" not in data:
            cat = data.pop("category")
            if isinstance(cat, int):
                data["category_id"] = cat
        super().__init__(**data)

class ProductVariation(SQLModel, table=True):
    __tablename__ = "product_variations"
    __table_args__ = {"extend_existing": True}

    id: Optional[int] = Field(default=None, primary_key=True)
    product_id: int = Field(foreign_key="products.articul")
    name: str
    price: float
    stock: int = Field(default=0)

class Receipt(SQLModel, table=True):
    __tablename__ = "receipts"
    __table_args__ = {"extend_existing": True}

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

    @property
    def status_title(self) -> str:
        return ""

    @status_title.setter
    def status_title(self, val: str):
        pass

    def __init__(self, **data):
        if "status_title" in data:
            data.pop("status_title")
        if "status" in data and "status_id" not in data:
            data["status_id"] = data.pop("status")
        super().__init__(**data)

class ReceiptItem(SQLModel, table=True):
    __tablename__ = "receipt_items"
    __table_args__ = {"extend_existing": True}

    id: Optional[int] = Field(default=None, primary_key=True)
    receipt_id: int = Field(foreign_key="receipts.receipt_id")
    product_id: int = Field(foreign_key="products.articul")
    variation_id: Optional[int] = Field(default=None, foreign_key="product_variations.id")
    quantity: int = Field(default=1)
    price_at_purchase: float = Field(default=0.0)

class Review(SQLModel, table=True):
    __tablename__ = "reviews"
    __table_args__ = {"extend_existing": True}

    id: Optional[int] = Field(default=None, primary_key=True)
    product_id: int = Field(foreign_key="products.articul")
    rating: int = Field(default=5)
    date: str = Field(default="")
    comment: str = Field(default="")
    user_id: Optional[int] = Field(default=None, foreign_key="users.user_id")

    @property
    def articul(self) -> int:
        return self.product_id

    @articul.setter
    def articul(self, val: int):
        self.product_id = val

    @property
    def user_name(self) -> str:
        return ""

    @user_name.setter
    def user_name(self, val: str):
        pass

    def __init__(self, **data):
        if "articul" in data and "product_id" not in data:
            data["product_id"] = data.pop("articul")
        elif "articul" in data:
            data.pop("articul")
        if "user_name" in data:
            data.pop("user_name")
        super().__init__(**data)

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
