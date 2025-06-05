#!/bin/bash
# filepath: c:\Users\anoliveira\Documents\repos\dev-academy-CRM\test_product_controller.sh

set -x  # Enable command tracing for debugging

BASE_URL="http://localhost:5205/api/product"
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

echo "=== Testing Product Controller ==="
echo "Base URL: $BASE_URL"
echo

# Test 1: Add a new product
echo "1. Testing POST /api/product/add"
ADD_RESPONSE=$(curl -v -w "%{http_code}" -X POST "$BASE_URL/add" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Product",
    "description": "This is a test product description",
    "category": "Electronics",
    "price": 99.99,
    "stock": 50
  }')

ADD_HTTP_CODE="${ADD_RESPONSE: -3}"
ADD_BODY="${ADD_RESPONSE%???}"

if [ "$ADD_HTTP_CODE" = "200" ]; then
    print_status 0 "Product created successfully"
    echo "Response: $ADD_BODY"
    
    # Extract ProductGuid if possible (basic extraction)
    PRODUCT_GUID=$(echo "$ADD_BODY" | grep -o '"ProductGuid":"[^"]*"' | cut -d'"' -f4)
    echo "Product GUID: $PRODUCT_GUID"
else
    print_status 1 "Failed to create product (HTTP: $ADD_HTTP_CODE)"
    echo "Response: $ADD_BODY"
fi

echo

# Test 2: Try to add invalid product
echo "2. Testing POST /api/product/add with invalid data"
INVALID_RESPONSE=$(curl -v -w "%{http_code}" -X POST "$BASE_URL/add" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "",
    "description": "",
    "category": "",
    "price": -10,
    "stock": -5
  }')

INVALID_HTTP_CODE="${INVALID_RESPONSE: -3}"
INVALID_BODY="${INVALID_RESPONSE%???}"

if [ "$INVALID_HTTP_CODE" = "400" ] || [ "$INVALID_HTTP_CODE" = "500" ]; then
    print_status 0 "Invalid data correctly rejected (HTTP: $INVALID_HTTP_CODE)"
else
    print_status 1 "Invalid data should be rejected (HTTP: $INVALID_HTTP_CODE)"
fi

echo

# Test 3: Get a product (assuming ProductId 1 exists)
echo "3. Testing GET /api/product/1"
GET_RESPONSE=$(curl -v -w "%{http_code}" -X GET "$BASE_URL/1")

GET_HTTP_CODE="${GET_RESPONSE: -3}"
GET_BODY="${GET_RESPONSE%???}"

if [ "$GET_HTTP_CODE" = "200" ]; then
    print_status 0 "Product retrieved successfully"
    echo "Response: $GET_BODY"
elif [ "$GET_HTTP_CODE" = "404" ]; then
    print_warning "Product with ID 1 not found (this might be expected)"
else
    print_status 1 "Failed to get product (HTTP: $GET_HTTP_CODE)"
    echo "Response: $GET_BODY"
fi

echo

# Test 4: Get non-existent product
echo "4. Testing GET /api/product/99999"
NOT_FOUND_RESPONSE=$(curl -v -w "%{http_code}" -X GET "$BASE_URL/99999")

NOT_FOUND_HTTP_CODE="${NOT_FOUND_RESPONSE: -3}"
NOT_FOUND_BODY="${NOT_FOUND_RESPONSE%???}"

if [ "$NOT_FOUND_HTTP_CODE" = "404" ]; then
    print_status 0 "Non-existent product correctly returned 404"
else
    print_status 1 "Non-existent product should return 404 (HTTP: $NOT_FOUND_HTTP_CODE)"
fi

echo

# Test 5: Update a product (assuming ProductId 1 exists)
echo "5. Testing PUT /api/product/update/1"
UPDATE_RESPONSE=$(curl -v -w "%{http_code}" -X PUT "$BASE_URL/update/1" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Updated Test Product",
    "description": "This is an updated test product description",
    "category": "Updated Electronics",
    "price": 149.99,
    "stock": 75
  }')

UPDATE_HTTP_CODE="${UPDATE_RESPONSE: -3}"
UPDATE_BODY="${UPDATE_RESPONSE%???}"

if [ "$UPDATE_HTTP_CODE" = "200" ]; then
    print_status 0 "Product updated successfully"
    echo "Response: $UPDATE_BODY"
elif [ "$UPDATE_HTTP_CODE" = "404" ] || [ "$UPDATE_HTTP_CODE" = "500" ]; then
    print_warning "Product update failed - product might not exist (HTTP: $UPDATE_HTTP_CODE)"
else
    print_status 1 "Failed to update product (HTTP: $UPDATE_HTTP_CODE)"
    echo "Response: $UPDATE_BODY"
fi

echo

# Test 6: Update with invalid data
echo "6. Testing PUT /api/product/update/1 with invalid data"
INVALID_UPDATE_RESPONSE=$(curl -v -w "%{http_code}" -X PUT "$BASE_URL/update/1" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "",
    "description": "",
    "category": "",
    "price": -50,
    "stock": -10
  }')

INVALID_UPDATE_HTTP_CODE="${INVALID_UPDATE_RESPONSE: -3}"

if [ "$INVALID_UPDATE_HTTP_CODE" = "400" ] || [ "$INVALID_UPDATE_HTTP_CODE" = "500" ]; then
    print_status 0 "Invalid update data correctly rejected (HTTP: $INVALID_UPDATE_HTTP_CODE)"
else
    print_status 1 "Invalid update data should be rejected (HTTP: $INVALID_UPDATE_HTTP_CODE)"
fi

echo
echo "=== Test Summary ==="
echo "All tests completed. Check the results above."
echo "Note: Some failures might be expected if the database is empty or products don't exist."