from .products import router as products_router
from .categories import router as categories_router
from .manufacturers import router as manufacturers_router
from .statuses import router as statuses_router
from .reviews import router as reviews_router
from .users import router as users_router
from .roles import router as roles_router
from .cart import router as cart_router
from .receipts import router as receipts_router

all_routers = [
    products_router,
    categories_router,
    manufacturers_router,
    statuses_router,
    reviews_router,
    users_router,
    roles_router,
    cart_router,
    receipts_router,
]

def register_controllers(app):
    for router in all_routers:
        app.include_router(router)
