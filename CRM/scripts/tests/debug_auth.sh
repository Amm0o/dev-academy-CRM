#!/bin/bash

# Simple debug script to test authentication

BASE_URL="http://localhost:5205/api"

echo "=== Debug Authentication ==="
echo "Base URL: $BASE_URL"
echo

# Test simple curl first
echo "1. Testing simple curl:"
response=$(curl -s -w "\n%{http_code}" -X POST "$BASE_URL/auth/login" \
    -H "Content-Type: application/json" \
    -d '{"email":"testuser@example.com","password":"StrongPassword123!"}')

status=$(echo "$response" | tail -n1)
body=$(echo "$response" | head -n -1)

echo "Status: $status"
echo "Body: $body"
echo

# Extract token
token=$(echo "$body" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
echo "Extracted Token: ${token:0:50}..."
echo

if [ "$status" = "200" ] && [ -n "$token" ]; then
    echo "✅ Authentication working correctly!"
    
    # Test using the token
    echo ""
    echo "2. Testing authenticated request:"
    user_response=$(curl -s -w "\n%{http_code}" -X GET "$BASE_URL/User/email/testuser@example.com" \
        -H "Authorization: Bearer $token")
    
    user_status=$(echo "$user_response" | tail -n1)
    user_body=$(echo "$user_response" | head -n -1)
    
    echo "User Status: $user_status"
    echo "User Body: $user_body"
    
    if [ "$user_status" = "200" ]; then
        echo "✅ Authenticated request working!"
    else
        echo "❌ Authenticated request failed"
    fi
else
    echo "❌ Authentication failed"
    echo "Status: $status, Token length: ${#token}"
fi
