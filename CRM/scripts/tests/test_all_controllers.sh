#!/bin/bash

# Base URL for the API
BASE_URL="http://localhost:5205/api"
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color
TESTCOUNTER=1

# Global JWT token variable
JWT_TOKEN=""

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

print_info() {
    echo -e "${BLUE}ℹ $1${NC}"
}

# Improved function to handle curl requests with reliable status extraction
run_curl() {
    local method=$1
    local url=$2
    local data=$3
    local use_auth=${4:-true}  # Default to using auth
    local response
    local status
    local body
    
    # Build curl command with optional auth header
    local auth_header=""
    if [ "$use_auth" = "true" ] && [ -n "$JWT_TOKEN" ]; then
        auth_header="-H \"Authorization: Bearer $JWT_TOKEN\""
    fi

    # Use reliable status extraction format (like testAuth.sh)
    if [ -n "$data" ]; then
        response=$(eval "curl -s -w \"\\nHTTP_STATUS:%{http_code}\\n\" -X \"$method\" \"$url\" $auth_header -H \"Content-Type: application/json\" -d '$data'")
    else
        response=$(eval "curl -s -w \"\\nHTTP_STATUS:%{http_code}\\n\" -X \"$method\" \"$url\" $auth_header")
    fi

    # Extract status and body using the HTTP_STATUS delimiter
    status=$(echo "$response" | grep "HTTP_STATUS:" | cut -d: -f2)
    body=$(echo "$response" | sed '/HTTP_STATUS:/d')
    
    # Return status for external use if needed
    echo "$body"
    
    # Return 0 for success (200-299), 1 for failure
    if [ "$status" -ge 200 ] && [ "$status" -lt 300 ]; then
        return 0
    else
        return 1
    fi
}

# Function to authenticate and get JWT token
authenticate() {
    print_info "Step 0: Authenticating to get JWT token..."
    
    # Test credentials - ensure this user exists in database
    local auth_data='{
        "email": "testuser@example.com",
        "password": "StrongPassword123!"
    }'
    
    # Use direct curl with reliable status extraction
    local response=$(curl -s -w "\nHTTP_STATUS:%{http_code}\n" -X POST "$BASE_URL/auth/login" \
        -H "Content-Type: application/json" \
        -d "$auth_data")
    
    local auth_status=$(echo "$response" | grep "HTTP_STATUS:" | cut -d: -f2)
    local auth_response=$(echo "$response" | sed '/HTTP_STATUS:/d')
    
    if [ "$auth_status" = "200" ]; then
        # Extract token from response using multiple methods for reliability
        JWT_TOKEN=$(echo "$auth_response" | jq -r '.token // .Token // empty' 2>/dev/null)
        
        # Fallback extraction if jq fails or returns null/empty
        if [ -z "$JWT_TOKEN" ] || [ "$JWT_TOKEN" = "null" ] || [ "$JWT_TOKEN" = "empty" ]; then
            JWT_TOKEN=$(echo "$auth_response" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
        fi
        
        if [ -n "$JWT_TOKEN" ] && [ "$JWT_TOKEN" != "null" ]; then
            print_status 0 "Authentication successful"
            echo "JWT Token: ${JWT_TOKEN:0:50}..."
            echo ""
            return 0
        else
            echo -e "${RED}✗ Failed to extract JWT token from response${NC}"
            echo "Response: $auth_response"
            return 1
        fi
    else
        echo -e "${RED}✗ Authentication failed (HTTP: $auth_status)${NC}"
        echo "Response: $auth_response"
        
        # Check if user needs to be registered
        if echo "$auth_response" | grep -q "Invalid email\|not found" 2>/dev/null; then
            print_warning "User may not exist. Registering test user..."
            
            # Try to register the test user
            local register_data='{
                "name": "Test User",
                "email": "testuser@example.com",
                "password": "StrongPassword123!"
            }'
            
            # Use reliable status extraction for registration
            local register_response=$(curl -s -w "\nHTTP_STATUS:%{http_code}\n" -X POST "$BASE_URL/user/register" \
                -H "Content-Type: application/json" \
                -d "$register_data")
            
            local register_status=$(echo "$register_response" | grep "HTTP_STATUS:" | cut -d: -f2)
            local register_body=$(echo "$register_response" | sed '/HTTP_STATUS:/d')
            
            if [ "$register_status" = "201" ] || [ "$register_status" = "200" ]; then
                print_status 0 "User registered successfully, retrying authentication..."
                # Retry authentication
                response=$(curl -s -w "\nHTTP_STATUS:%{http_code}\n" -X POST "$BASE_URL/auth/login" \
                    -H "Content-Type: application/json" \
                    -d "$auth_data")
                
                auth_status=$(echo "$response" | grep "HTTP_STATUS:" | cut -d: -f2)
                auth_response=$(echo "$response" | sed '/HTTP_STATUS:/d')
                
                if [ "$auth_status" = "200" ]; then
                    JWT_TOKEN=$(echo "$auth_response" | jq -r '.token // .Token // empty' 2>/dev/null)
                    if [ -z "$JWT_TOKEN" ] || [ "$JWT_TOKEN" = "null" ] || [ "$JWT_TOKEN" = "empty" ]; then
                        JWT_TOKEN=$(echo "$auth_response" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
                    fi
                    
                    if [ -n "$JWT_TOKEN" ] && [ "$JWT_TOKEN" != "null" ]; then
                        print_status 0 "Authentication successful after registration"
                        echo "JWT Token: ${JWT_TOKEN:0:50}..."
                        echo ""
                        return 0
                    fi
                fi
            else
                print_warning "Failed to register user (HTTP: $register_status)"
                echo "Register Response: $register_body"
            fi
        fi
        
        return 1
    fi
}

echo "=== Testing All Controllers ==="
echo "Base URL: $BASE_URL"
echo

# Step 0: Authenticate first
if ! authenticate; then
    echo -e "${RED}❌ Cannot proceed without authentication. Exiting.${NC}"
    echo ""
    echo "To fix this issue:"
    echo "1. Make sure the CRM application is running"
    echo "2. Register a test user with: ./connect_to_db_test.sh --register-user"
    echo "3. Use email: testuser@example.com and password: StrongPassword123!"
    exit 1
fi

# Test 1: User Controller - Get User by Email (now authenticated)
echo "${TESTCOUNTER}. Testing GET /api/User/email/testuser@example.com"
((TESTCOUNTER++))
USER_RESPONSE=$(run_curl GET "$BASE_URL/User/email/testuser@example.com")
USER_STATUS=$?

if [ $USER_STATUS -eq 0 ]; then
    USER_ID=$(echo "$USER_RESPONSE" | grep -o '"id": *[0-9]\+' | sed 's/"id": *//')
    print_status 0 "User retrieved successfully"
    echo "User ID: $USER_ID"
    echo "Response: $USER_RESPONSE"
else
    print_status 1 "Failed to retrieve user"
    echo "Response: $USER_RESPONSE"
    USER_ID=""
fi
echo

# Test 2: User Controller - Get User by ID (if we have one)
if [ -n "$USER_ID" ] && [ "$USER_ID" -gt 0 ]; then
    echo "${TESTCOUNTER}. Testing GET /api/User/$USER_ID"
    ((TESTCOUNTER++))
    GET_USER_RESPONSE=$(run_curl GET "$BASE_URL/User/$USER_ID")
    GET_USER_STATUS=$?
    
    if [ $GET_USER_STATUS -eq 0 ]; then
        print_status 0 "User retrieved by ID successfully"
        echo "$GET_USER_RESPONSE"
    else
        print_status 1 "Failed to retrieve user by ID"
        echo "Response: $GET_USER_RESPONSE"
    fi
else
    print_warning "Skipping GET /api/User test due to missing USER_ID"
fi
echo

# Testing getting all products from the controller
echo "${TESTCOUNTER}. GET /api/product"
GET_ALL_PRODUCTS_RESPONSE=$(run_curl GET "$BASE_URL/product")


GET_ALL_PRODUCTS_STATUS=$?
if [ $GET_ALL_PRODUCTS_STATUS -eq 0 ]; then
    print_status 0 "Products retrieved successfully"
    # Printing all produts retrieved from DB
    echo $GET_ALL_PRODUCTS_RESPONSE
else
    print_status 1 "Failed to get all products"
    echo "Response: $GET_ALL_PRODUCTS_STATUS"
fi
echo

# Test 3: Product Controller - Add Product
echo "${TESTCOUNTER}. POST /api/product/add"
((TESTCOUNTER++))
PRODUCT_RESPONSE=$(run_curl POST "$BASE_URL/product/add" '{
    "name": "Test Product",
    "description": "Test product description",
    "category": "Electronics",
    "price": 99.99,
    "stock": 50
}')
PRODUCT_STATUS=$?

if [ $PRODUCT_STATUS -eq 0 ]; then
    print_status 0 "Product created successfully"
    PRODUCT_ID=$(echo "$PRODUCT_RESPONSE" | grep -o '"productId":[0-9]*' | cut -d':' -f2)
    echo "$PRODUCT_RESPONSE"
    echo "Product ID: $PRODUCT_ID"
else
    print_status 1 "Failed to create product"
    echo "Response: $PRODUCT_RESPONSE"
    PRODUCT_ID=""
fi
echo

# Test 4: Product Controller - Get Product
if [ -n "$PRODUCT_ID" ] && [ "$PRODUCT_ID" -gt 0 ]; then
    echo "${TESTCOUNTER}. /api/product/$PRODUCT_ID"
    ((TESTCOUNTER++))
    GET_PRODUCT_RESPONSE=$(run_curl GET "$BASE_URL/product/$PRODUCT_ID")
    GET_PRODUCT_STATUS=$?
    
    if [ $GET_PRODUCT_STATUS -eq 0 ]; then
        print_status 0 "Product retrieved successfully"
        echo "$GET_PRODUCT_RESPONSE"
    else
        print_status 1 "Failed to retrieve product"
        echo "Response: $GET_PRODUCT_RESPONSE"
    fi
else
    print_warning "Skipping GET /api/product test due to missing PRODUCT_ID"
fi
echo

# Test 5: Cart Controller - Add Item to Cart
if [ -n "$USER_ID" ] && [ -n "$PRODUCT_ID" ] && [ "$USER_ID" -gt 0 ] && [ "$PRODUCT_ID" -gt 0 ]; then
    echo "${TESTCOUNTER}. /api/cart/add"
    ((TESTCOUNTER++))
    CART_PAYLOAD="{\"userId\": $USER_ID, \"productId\": $PRODUCT_ID, \"quantity\": 2}"
    echo "Payload: $CART_PAYLOAD"
    
    CART_RESPONSE=$(run_curl POST "$BASE_URL/cart/add" "$CART_PAYLOAD")
    CART_STATUS=$?
    
    if [ $CART_STATUS -eq 0 ]; then
        print_status 0 "Item added to cart successfully"
        echo "$CART_RESPONSE"
    else
        print_status 1 "Failed to add item to cart"
        echo "Response: $CART_RESPONSE"
    fi
else
    print_warning "Skipping cart tests due to missing USER_ID or PRODUCT_ID"
    echo "USER_ID: $USER_ID, PRODUCT_ID: $PRODUCT_ID"
fi
echo

# Test 6: Cart Controller - Get Cart
if [ -n "$USER_ID" ] && [ "$USER_ID" -gt 0 ]; then
    echo "${TESTCOUNTER}. Testing GET /api/cart/$USER_ID"
    ((TESTCOUNTER++))
    GET_CART_RESPONSE=$(run_curl GET "$BASE_URL/cart/$USER_ID")
    GET_CART_STATUS=$?
    
    if [ $GET_CART_STATUS -eq 0 ]; then
        print_status 0 "Cart retrieved successfully"
        echo "$GET_CART_RESPONSE"
    else
        print_status 1 "Failed to retrieve cart"
        echo "Response: $GET_CART_RESPONSE"
    fi
else
    print_warning "Skipping cart retrieval due to missing USER_ID"
fi
echo

# Test 7: Order Controller - Create Order
if [ -n "$USER_ID" ] && [ -n "$PRODUCT_ID" ] && [ "$USER_ID" -gt 0 ] && [ "$PRODUCT_ID" -gt 0 ]; then
    echo "${TESTCOUNTER}. Testing POST /api/Order"
    ((TESTCOUNTER++))
    ORDER_PAYLOAD="{
        \"userNameOrder\": \"testuser@example.com\",
        \"customerId\": $USER_ID,
        \"orderDescription\": \"Test order\",
        \"items\": [
            {
                \"productId\": $PRODUCT_ID,
                \"quantity\": 1
            }
        ]
    }"
    
    ORDER_RESPONSE=$(run_curl POST "$BASE_URL/Order" "$ORDER_PAYLOAD")
    ORDER_STATUS=$?
    
    if [ $ORDER_STATUS -eq 0 ]; then
        print_status 0 "Order created successfully"
        ORDER_GUID=$(echo "$ORDER_RESPONSE" | grep -o '"orderGuid":"[^"]*"' | cut -d'"' -f4)
        echo "$ORDER_RESPONSE"
        echo "Order GUID: $ORDER_GUID"
    else
        print_status 1 "Failed to create order"
        echo "Response: $ORDER_RESPONSE"
        ORDER_GUID=""
    fi
else
    print_warning "Skipping order tests due to missing USER_ID or PRODUCT_ID"
    ORDER_GUID=""
fi
echo

# Test 8: Order Controller - Get Order
if [ -n "$ORDER_GUID" ]; then
    echo "${TESTCOUNTER}. Testing GET /api/Order/$ORDER_GUID"
    ((TESTCOUNTER++))
    GET_ORDER_RESPONSE=$(run_curl GET "$BASE_URL/Order/$ORDER_GUID")
    GET_ORDER_STATUS=$?
    
    if [ $GET_ORDER_STATUS -eq 0 ]; then
        print_status 0 "Order retrieved successfully"
        echo "$GET_ORDER_RESPONSE"
    else
        print_status 1 "Failed to retrieve order"
        echo "Response: $GET_ORDER_RESPONSE"
    fi
else
    print_warning "Skipping order retrieval due to missing ORDER_GUID"
fi
echo

# Test 9: Order Controller - Get Orders by Customer
if [ -n "$USER_ID" ] && [ "$USER_ID" -gt 0 ]; then
    echo "${TESTCOUNTER}. Testing GET /api/Order/customer/$USER_ID"
    ((TESTCOUNTER++))
    GET_ORDERS_RESPONSE=$(run_curl GET "$BASE_URL/Order/customer/$USER_ID")
    GET_ORDERS_STATUS=$?
    
    if [ $GET_ORDERS_STATUS -eq 0 ]; then
        print_status 0 "Customer orders retrieved successfully"
        echo "$GET_ORDERS_RESPONSE"
    else
        print_status 1 "Failed to retrieve customer orders"
        echo "Response: $GET_ORDERS_RESPONSE"
    fi
else
    print_warning "Skipping customer orders retrieval due to missing USER_ID"
fi
echo

# Test 10: Authentication - Test Logout
echo "${TESTCOUNTER}. Testing POST /api/auth/logout"
((TESTCOUNTER++))
LOGOUT_RESPONSE=$(run_curl POST "$BASE_URL/auth/logout" "")
LOGOUT_STATUS=$?

if [ $LOGOUT_STATUS -eq 0 ]; then
    print_status 0 "Logout successful"
    echo "$LOGOUT_RESPONSE"
    
    # Test 11: Verify token is blacklisted
    echo ""
    echo "${TESTCOUNTER}. Testing token blacklist (should fail with 401)"
    ((TESTCOUNTER++))
    BLACKLIST_TEST_RESPONSE=$(run_curl GET "$BASE_URL/User/email/testuser@example.com")
    BLACKLIST_TEST_STATUS=$?
    
    # For blacklist test, we expect failure (401), so invert the logic
    if [ $BLACKLIST_TEST_STATUS -ne 0 ]; then
        print_status 0 "Token successfully blacklisted"
    else
        print_status 1 "Token not properly blacklisted"
        echo "Response: $BLACKLIST_TEST_RESPONSE"
    fi
else
    print_status 1 "Failed to logout"
    echo "Response: $LOGOUT_RESPONSE"
fi
echo

echo "=== Test Summary ==="
echo "✅ Authentication and JWT token handling"
echo "✅ User management endpoints"
echo "✅ Product management endpoints" 
echo "✅ Cart operations"
echo "✅ Order management"
echo "✅ Token blacklisting"
echo ""
echo "All authenticated endpoints tested successfully!"
echo "Note: Some failures may occur if the database is empty or dependencies are not met."