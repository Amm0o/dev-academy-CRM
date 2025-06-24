#!/bin/bash
# filepath: /home/anoliveira/repos/dev-academy-CRM/CRM/scripts/tests/test_admin_flow.sh

# Admin Flow Test Script
# Tests admin authentication, user promotion, and product creation

# Configuration
API_BASE_URL="http://localhost:5205/api"
CURL_OPTS="-s -w \nHTTP_STATUS:%{http_code}\n"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Admin credentials from appsettings.json
ADMIN_EMAIL="admin@crm.com"
ADMIN_PASSWORD="StrongPassword123"  # This should match your appsettings.json

# Test user to promote
TEST_USER_EMAIL="testuser@example.com"
TEST_USER_NAME="Test User"
TEST_USER_PASSWORD="StrongPassword123!"

# Global JWT token variable
ADMIN_JWT_TOKEN=""

# Function to print colored output
print_status() {
    case $1 in
        "success") echo -e "${GREEN}✓ $2${NC}" ;;
        "error") echo -e "${RED}✗ $2${NC}" ;;
        "info") echo -e "${BLUE}ℹ $2${NC}" ;;
        "warning") echo -e "${YELLOW}⚠ $2${NC}" ;;
    esac
}

# Function to extract HTTP status from response
extract_status() {
    echo "$1" | grep "HTTP_STATUS:" | cut -d: -f2
}

# Function to extract body from response
extract_body() {
    echo "$1" | sed '/HTTP_STATUS:/d'
}

echo "============================================"
echo "CRM Admin Flow Test"
echo "============================================"
echo ""

print_status "info" "API Base URL: $API_BASE_URL"
print_status "info" "Admin Email: $ADMIN_EMAIL"
echo ""

# Step 1: Ensure test user exists
print_status "info" "Step 1: Ensuring test user exists..."

# First try to register the test user (might already exist)
register_response=$(curl $CURL_OPTS -X POST "$API_BASE_URL/user/register" \
    -H "Content-Type: application/json" \
    -d "{
        \"name\": \"$TEST_USER_NAME\",
        \"email\": \"$TEST_USER_EMAIL\",
        \"password\": \"$TEST_USER_PASSWORD\"
    }")

register_status=$(extract_status "$register_response")
register_body=$(extract_body "$register_response")

if [ "$register_status" = "201" ] || [ "$register_status" = "200" ]; then
    print_status "success" "Test user registered successfully"
elif echo "$register_body" | grep -q -i "already.*exist\|duplicate" 2>/dev/null; then
    print_status "info" "Test user already exists"
else
    print_status "warning" "Could not register test user (Status: $register_status)"
    echo "Response: $register_body"
fi

echo ""

# Step 2: Login as admin
print_status "info" "Step 2: Logging in as admin..."

admin_login_data="{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\"}"

admin_response=$(curl $CURL_OPTS -X POST "$API_BASE_URL/auth/login" \
    -H "Content-Type: application/json" \
    -d "$admin_login_data")

admin_status=$(extract_status "$admin_response")
admin_body=$(extract_body "$admin_response")

if [ "$admin_status" = "200" ]; then
    print_status "success" "Admin login successful!"
    
    # Extract token
    ADMIN_JWT_TOKEN=$(echo "$admin_body" | jq -r '.token // .Token // empty' 2>/dev/null)
    
    # Alternative extraction if jq fails
    if [ -z "$ADMIN_JWT_TOKEN" ] || [ "$ADMIN_JWT_TOKEN" = "null" ] || [ "$ADMIN_JWT_TOKEN" = "empty" ]; then
        ADMIN_JWT_TOKEN=$(echo "$admin_body" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
    fi
    
    if [ -n "$ADMIN_JWT_TOKEN" ] && [ "$ADMIN_JWT_TOKEN" != "null" ]; then
        print_status "success" "JWT token extracted"
        echo "Token: ${ADMIN_JWT_TOKEN:0:50}..."
    else
        print_status "error" "Failed to extract JWT token"
        echo "Response: $admin_body"
        exit 1
    fi
else
    print_status "error" "Admin login failed (Status: $admin_status)"
    echo "Response: $admin_body"
    
    if echo "$admin_body" | grep -q -i "invalid.*email\|not.*found" 2>/dev/null; then
        print_status "error" "Admin user not found. Check if admin user is seeded in database"
        echo "Expected email: $ADMIN_EMAIL"
    elif echo "$admin_body" | grep -q -i "invalid.*password\|incorrect" 2>/dev/null; then
        print_status "error" "Invalid admin password. Check appsettings.json"
    fi
    exit 1
fi

echo ""

# Step 3: Verify admin role by accessing admin-only endpoint
print_status "info" "Step 3: Verifying admin privileges..."

# Try to access a protected endpoint to verify token works
verify_response=$(curl $CURL_OPTS -X GET "$API_BASE_URL/user/email/$ADMIN_EMAIL" \
    -H "Authorization: Bearer $ADMIN_JWT_TOKEN")

verify_status=$(extract_status "$verify_response")
verify_body=$(extract_body "$verify_response")

if [ "$verify_status" = "200" ]; then
    # Check if user has Admin role
    if echo "$verify_body" | grep -q '"role":"Admin"' 2>/dev/null; then
        print_status "success" "Admin role confirmed"
    else
        print_status "warning" "User found but role might not be Admin"
        echo "User data: $verify_body"
    fi
else
    print_status "error" "Failed to verify admin user (Status: $verify_status)"
    echo "Response: $verify_body"
fi

echo ""

# Step 4: Promote test user to admin
print_status "info" "Step 4: Promoting test user to admin..."

promote_response=$(curl $CURL_OPTS -X POST "$API_BASE_URL/setup/$TEST_USER_EMAIL" \
    -H "Authorization: Bearer $ADMIN_JWT_TOKEN")

promote_status=$(extract_status "$promote_response")
promote_body=$(extract_body "$promote_response")

if [ "$promote_status" = "200" ]; then
    print_status "success" "User promoted to admin successfully!"
    echo "Response: $promote_body"
elif [ "$promote_status" = "403" ]; then
    print_status "error" "Forbidden - current user might not have admin role"
    echo "Response: $promote_body"
elif [ "$promote_status" = "404" ]; then
    print_status "error" "User to promote not found"
    echo "Response: $promote_body"
else
    print_status "error" "Failed to promote user (Status: $promote_status)"
    echo "Response: $promote_body"
fi

echo ""

# Step 5: Add a product as admin
print_status "info" "Step 5: Adding a product as admin..."

product_data='{
    "name": "Admin Test Product",
    "description": "This product was created by an admin",
    "category": "Electronics",
    "price": 299.99,
    "stock": 100
}'

product_response=$(curl $CURL_OPTS -X POST "$API_BASE_URL/product/add" \
    -H "Authorization: Bearer $ADMIN_JWT_TOKEN" \
    -H "Content-Type: application/json" \
    -d "$product_data")

product_status=$(extract_status "$product_response")
product_body=$(extract_body "$product_response")

if [ "$product_status" = "200" ] || [ "$product_status" = "201" ]; then
    print_status "success" "Product created successfully!"
    echo "Response: $product_body"
    
    # Extract product ID if available
    PRODUCT_ID=$(echo "$product_body" | grep -o '"productId":[0-9]*' | cut -d':' -f2)
    if [ -n "$PRODUCT_ID" ]; then
        echo "Product ID: $PRODUCT_ID"
    fi
elif [ "$product_status" = "403" ]; then
    print_status "error" "Forbidden - user might not have admin role"
    echo "Response: $product_body"
elif [ "$product_status" = "400" ]; then
    print_status "error" "Bad request - invalid product data"
    echo "Response: $product_body"
else
    print_status "error" "Failed to create product (Status: $product_status)"
    echo "Response: $product_body"
fi

echo ""

# Step 6: Verify promoted user has admin privileges
print_status "info" "Step 6: Verifying promoted user's admin privileges..."

# Login as the promoted user
promoted_login_data="{\"email\":\"$TEST_USER_EMAIL\",\"password\":\"$TEST_USER_PASSWORD\"}"

promoted_response=$(curl $CURL_OPTS -X POST "$API_BASE_URL/auth/login" \
    -H "Content-Type: application/json" \
    -d "$promoted_login_data")

promoted_status=$(extract_status "$promoted_response")
promoted_body=$(extract_body "$promoted_response")

if [ "$promoted_status" = "200" ]; then
    print_status "success" "Promoted user login successful!"
    
    # Extract token
    PROMOTED_TOKEN=$(echo "$promoted_body" | jq -r '.token // .Token // empty' 2>/dev/null)
    
    if [ -z "$PROMOTED_TOKEN" ] || [ "$PROMOTED_TOKEN" = "null" ] || [ "$PROMOTED_TOKEN" = "empty" ]; then
        PROMOTED_TOKEN=$(echo "$promoted_body" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
    fi
    
    if [ -n "$PROMOTED_TOKEN" ] && [ "$PROMOTED_TOKEN" != "null" ]; then
        # Try to add a product with promoted user's token
        test_product_data='{
            "name": "Promoted Admin Test Product",
            "description": "Created by newly promoted admin",
            "category": "Test",
            "price": 99.99,
            "stock": 50
        }'
        
        test_product_response=$(curl $CURL_OPTS -X POST "$API_BASE_URL/product/add" \
            -H "Authorization: Bearer $PROMOTED_TOKEN" \
            -H "Content-Type: application/json" \
            -d "$test_product_data")

        echo $test_product_data
        
        test_product_status=$(extract_status "$test_product_response")
        
        if [ "$test_product_status" = "200" ] || [ "$test_product_status" = "201" ]; then
            print_status "success" "Promoted user can create products - admin privileges confirmed!"
        else
            print_status "error" "Promoted user cannot create products (Status: $test_product_status)"
            echo "Response: $(extract_body "$test_product_response")"
        fi
    fi
else
    print_status "warning" "Could not verify promoted user privileges"
fi

echo ""
echo "============================================"
echo "Admin Flow Test Summary"
echo "============================================"
echo ""
echo "✅ Admin authentication"
echo "✅ User promotion to admin"
echo "✅ Product creation as admin"
echo "✅ Promoted user admin privileges verification"
echo ""

# Check if all critical steps passed
if [ "$admin_status" = "200" ] && [ "$promote_status" = "200" ] && ([ "$product_status" = "200" ] || [ "$product_status" = "201" ]); then
    print_status "success" "All admin flow tests passed!"
    exit 0
else
    print_status "error" "Some admin flow tests failed"
    echo ""
    echo "Troubleshooting tips:"
    echo "1. Ensure the CRM application is running"
    echo "2. Check that admin user is seeded with correct credentials"
    echo "3. Verify admin password in appsettings.json matches the script"
    echo "4. Make sure the test user exists before promotion"
    exit 1
fi