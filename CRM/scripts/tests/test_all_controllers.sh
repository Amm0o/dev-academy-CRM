#!/bin/bash

# Base URL for the API
BASE_URL="http://localhost:5205/api"
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    if [ $1 -eq 0 ]; then
        echo -e "${GREEN}✓ $2${NC}"
    else
        echo -e "${RED}✗ $2${NC}"
    fi
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

# Function to handle curl requests
run_curl() {
    local method=$1
    local url=$2
    local data=$3
    local response
    local status

    if [ -n "$data" ]; then
        response=$(curl -s -w "%{http_code}" -X "$method" "$url" -H "Content-Type: application/json" -d "$data")
    else
        response=$(curl -s -w "%{http_code}" -X "$method" "$url")
    fi

    status="${response: -3}"
    body="${response%???}"
    echo "$body"
    return $(($status))
}

echo "=== Testing All Controllers ==="
echo "Base URL: $BASE_URL"
echo

# Test 1: Cleanup - Delete existing test user if exists
echo "1. Testing DELETE /api/User for testuser@example.com"
USER_RESPONSE=$(run_curl GET "$BASE_URL/User/email/testuser@example.com")
USER_STATUS=$?
USER_ID=$(echo "$USER_RESPONSE" | grep -o '"id": *[0-9]\+' | sed 's/"id": *//')
echo "Retrieved User ID: $USER_ID"
if [ $USER_STATUS -eq 200 ] && [ -n "$USER_ID" ] && [ "$USER_ID" -gt 0 ]; then
    echo "Sending DELETE to $BASE_URL/User/$USER_ID"
    DELETE_RESPONSE=$(run_curl DELETE "$BASE_URL/User/$USER_ID")
    DELETE_STATUS=$?
    echo "Delete Response: $DELETE_RESPONSE"
    echo "Delete Status: $DELETE_STATUS"
    if [ $DELETE_STATUS -eq 200 ]; then
        print_status 0 "Existing test user deleted successfully"
    else
        print_warning "Failed to delete existing test user (HTTP: $DELETE_STATUS)"
        echo "Delete Response: $DELETE_RESPONSE"
    fi
else
    print_status 0 "No existing test user found, proceeding with registration"
    echo "GET User Status: $USER_STATUS, User ID: $USER_ID"
fi
echo

# Test 2: User Controller - Register User
echo "2. Testing POST /api/User/register"
USER_RESPONSE=$(run_curl POST "$BASE_URL/User/register" '{
    "name": "Test User",
    "email": "testuser@example.com",
    "password": "StrongPassword123!"
}')
USER_STATUS=$?
if [ $USER_STATUS -eq 201 ]; then
    print_status 0 "User registered successfully"
    USER_ID=$(echo "$USER_RESPONSE" | grep -o '"id": *[0-9]\+' | sed 's/"id": *//')
    echo $USER_RESPONSE
    echo "User ID: $USER_ID"
else
    print_status 1 "Failed to register user (HTTP: $USER_STATUS)"
    echo "Response: $USER_RESPONSE"
    USER_ID=""
fi
echo

# Test 3: User Controller - Get User by ID
if [ -n "$USER_ID" ]; then
    echo "3. Testing GET /api/User/$USER_ID"
    GET_USER_RESPONSE=$(run_curl GET "$BASE_URL/User/$USER_ID")
    GET_USER_STATUS=$?
    if [ $GET_USER_STATUS -eq 200 ]; then
        print_status 0 "User retrieved by ID successfully"
    else
        print_status 1 "Failed to retrieve user by ID (HTTP: $GET_USER_STATUS)"
        echo "Response: $GET_USER_RESPONSE"
    fi
else
    print_warning "Skipping GET /api/User test due to missing USER_ID"
fi
echo

# Test 4: User Controller - Get User by Email
echo "4. Testing GET /api/User/email/testuser@example.com"
GET_EMAIL_RESPONSE=$(run_curl GET "$BASE_URL/User/email/testuser@example.com")
GET_EMAIL_STATUS=$?
if [ $GET_EMAIL_STATUS -eq 200 ]; then
    print_status 0 "User retrieved by email successfully"
    echo $GET_EMAIL_RESPONSE
else
    print_status 1 "Failed to retrieve user by email (HTTP: $GET_EMAIL_STATUS)"
    echo "Response: $GET_EMAIL_RESPONSE"
fi
echo

# Test 5: Product Controller - Add Product
echo "5. Testing POST /api/product/add"
PRODUCT_RESPONSE=$(run_curl POST "$BASE_URL/product/add" '{
    "name": "Test Product",
    "description": "Test product description",
    "category": "Electronics",
    "price": 99.99,
    "stock": 50
}')
PRODUCT_STATUS=$?
if [ $PRODUCT_STATUS -eq 200 ]; then
    print_status 0 "Product created successfully"
    PRODUCT_ID=$(echo "$PRODUCT_RESPONSE" | grep -o '"productId":[0-9]*' | cut -d':' -f2)
    echo $PRODUCT_RESPONSE
    echo "Product ID: $PRODUCT_ID"
else
    print_status 1 "Failed to create product (HTTP: $PRODUCT_STATUS)"
fi
echo

# Test 6: Product Controller - Get Product
echo "6. Testing GET /api/product/$PRODUCT_ID"
GET_PRODUCT_RESPONSE=$(run_curl GET "$BASE_URL/product/$PRODUCT_ID")
GET_PRODUCT_STATUS=$?
if [ $GET_PRODUCT_STATUS -eq 200 ]; then
    print_status 0 "Product retrieved successfully"
    echo $GET_PRODUCT_RESPONSE
else
    print_status 1 "Failed to retrieve product (HTTP: $GET_PRODUCT_STATUS)"
fi
echo

# Test 7: Cart Controller - Add Item to Cart
echo "7. Testing POST /api/cart/add"
echo "DEBUG: USER_ID='$USER_ID'"
echo "DEBUG: PRODUCT_ID='$PRODUCT_ID'"

# Generate the JSON payload first and show it
JSON_PAYLOAD=$(cat << EOF
{
    "userId": $USER_ID,
    "productId": $PRODUCT_ID,
    "quantity": 2
}
EOF
)

echo "DEBUG: JSON Payload:"
echo "$JSON_PAYLOAD"

CART_RESPONSE=$(run_curl POST "$BASE_URL/cart/add" "$JSON_PAYLOAD")
CART_STATUS=$?
if [ $CART_STATUS -eq 200 ]; then
    print_status 0 "Item added to cart successfully"
    echo $CART_RESPONSE
else
    echo $CART_RESPONSE 
    print_status 1 "Failed to add item to cart (HTTP: $CART_STATUS)"
fi
echo

# Test 8: Cart Controller - Get Cart
echo "8. Testing GET /api/cart/$USER_ID"
GET_CART_RESPONSE=$(run_curl GET "$BASE_URL/cart/$USER_ID")
GET_CART_STATUS=$?
if [ $GET_CART_STATUS -eq 200 ]; then
    print_status 0 "Cart retrieved successfully"
else
    print_status 1 "Failed to retrieve cart (HTTP: $GET_CART_STATUS)"
fi
echo

# Test 9: Clear Cart
echo "9. Test DELETE /api/cart/$USER_ID"
CLEAR_CART_RESPONSE=$(run_curl DELETE "$BASE_URL/cart/$USER_ID")
CLEAR_CART_STATUS=$?
if [ $CLEAR_CART_STATUS -eq 200 ]; then
    print_status 0 "Cart cleared succesffuly"
    echo $CLEAR_CART_RESPONSE
else 
    print_status 1 "Failed to clear cart (HTTP: $CLEAR_CART_STATUS)"
    echo $CLEAR_CART_RESPONSE
fi
echo

# Test 10: Order Controller - Create Order
echo "10. Testing POST /api/Order"
ORDER_RESPONSE=$(run_curl POST "$BASE_URL/Order" '{
    "userNameOrder": "testuser@example.com",
    "customerId": '"$USER_ID"',
    "orderDescription": "Test order",
    "items": [
        {
            "productId": '"$PRODUCT_ID"',
            "quantity": 1
        }
    ]
}')
ORDER_STATUS=$?
echo $ORDER_RESPONSE
if [ $ORDER_STATUS -eq 201 ]; then
    print_status 0 "Order created successfully"
    ORDER_GUID=$(echo "$ORDER_RESPONSE" | grep -o '"orderGuid":"[^"]*"' | cut -d'"' -f4)
    echo "Order GUID: $ORDER_GUID"
else
    print_status 1 "Failed to create order (HTTP: $ORDER_STATUS)"
fi
echo

# Test 11: Order Controller - Get Order
echo "11. Testing GET /api/Order/$ORDER_GUID"
GET_ORDER_RESPONSE=$(run_curl GET "$BASE_URL/Order/$ORDER_GUID")
GET_ORDER_STATUS=$?
echo $GET_ORDER_RESPONSE
if [ $GET_ORDER_STATUS -eq 200 ]; then
    print_status 0 "Order retrieved successfully"
else
    print_status 1 "Failed to retrieve order (HTTP: $GET_ORDER_STATUS)"
fi
echo

# Test 12: Order Controller - Get Orders by Customer
echo "12. Testing GET /api/Order/customer/$USER_ID"
GET_ORDERS_RESPONSE=$(run_curl GET "$BASE_URL/Order/customer/$USER_ID")
GET_ORDERS_STATUS=$?
echo $GET_ORDERS_RESPONSE
if [ $GET_ORDERS_STATUS -eq 200 ]; then
    print_status 0 "Customer orders retrieved successfully"
else
    print_status 1 "Failed to retrieve customer orders (HTTP: $GET_ORDERS_STATUS)"
fi
echo

echo "=== Test Summary ==="
echo "All tests completed. Check the results above."
echo "Note: Some failures may occur if the database is empty or dependencies are not met."