#!/bin/bash
# filepath: /root/repos/dev-academy-CRM/CRM/scripts/tests/test_cart.sh

API_URL="http://localhost:5205/api"
USER_ID=1  # Replace with actual user ID

# 1. Get cart (should be empty or show existing cart)
echo "Getting cart for user $USER_ID"
curl -s "$API_URL/cart/$USER_ID" | json_pp

# 2. Add item to cart
echo -e "\nAdding item to cart"
curl -s -X POST "$API_URL/cart/add" \
  -H "Content-Type: application/json" \
  -d '{
    "UserId": '$USER_ID',
    "ProductId": 1,
    "Quantity": 2
  }' | json_pp

# 3. Update item quantity
echo -e "\nUpdating item quantity"
curl -s -X PUT "$API_URL/cart/update" \
  -H "Content-Type: application/json" \
  -d '{
    "UserId": '$USER_ID',
    "ProductId": 1,
    "Quantity": 3
  }' | json_pp

# 4. Get updated cart
echo -e "\nGetting updated cart"
curl -s "$API_URL/cart/$USER_ID" | json_pp

# 5. Remove item from cart
echo -e "\nRemoving item from cart"
curl -s -X DELETE "$API_URL/cart/$USER_ID/item/1"

# 6. Get cart after removal
echo -e "\nGetting cart after item removal"
curl -s "$API_URL/cart/$USER_ID" | json_pp

# 7. Clear cart
echo -e "\nClearing cart"
curl -s -X DELETE "$API_URL/cart/$USER_ID"