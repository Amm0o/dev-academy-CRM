#!/bin/bash
API_URL="http://localhost:5205/api"
USER_ID=1

# Function to handle curl and JSON parsing
run_curl() {
    local method=$1
    local url=$2
    local data=$3
    local response
    local status

    if [ -n "$data" ]; then
        response=$(curl -s -X "$method" "$url" -H "Content-Type: application/json" -d "$data" -w "\n%{http_code}")
    else
        response=$(curl -s -X "$method" "$url" -w "\n%{http_code}")
    fi

    # Split response and status code
    status=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')

    # Check if response is JSON
    if [[ $status -eq 200 && $body == {* ]]; then
        echo "$body" | json_pp 2>/dev/null || echo "Failed to parse JSON: $body"
    else
        echo "Response (Status $status): $body"
    fi
}

# 1. Get cart
echo "Getting cart for user $USER_ID"
run_curl GET "$API_URL/cart/$USER_ID"

# 2. Add item to cart
echo -e "\nAdding item to cart"
run_curl POST "$API_URL/cart/add" '{
    "UserId": '"$USER_ID"',
    "ProductId": 1,
    "Quantity": 2
}'

# 3. Update item quantity
echo -e "\nUpdating item quantity"
run_curl PUT "$API_URL/cart/update" '{
    "UserId": '"$USER_ID"',
    "ProductId": 1,
    "Quantity": 3
}'

# 4. Get updated cart
echo -e "\nGetting updated cart"
run_curl GET "$API_URL/cart/$USER_ID"

# 5. Remove item from cart
echo -e "\nRemoving item from cart"
run_curl DELETE "$API_URL/cart/$USER_ID/item/1"

# 6. Get cart after removal
echo -e "\nGetting cart after item removal"
run_curl GET "$API_URL/cart/$USER_ID"

# 7. Clear cart
echo -e "\nClearing cart"
run_curl DELETE "$API_URL/cart/$USER_ID"