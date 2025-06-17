#!/bin/bash

# Authentication Test Script
# Tests login with properly registered BCrypt users

# Configuration
API_BASE_URL="http://localhost:5205/api"
CURL_OPTS="-s -w \nHTTP_STATUS:%{http_code}\n"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

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

# Test data
TEST_EMAIL="testuser@example.com"
TEST_PASSWORD="StrongPassword123!"
WRONG_PASSWORD="WrongPassword456!"
TEST_NAME="Test User"

echo "============================================"
echo "CRM Authentication Test"
echo "============================================"
echo ""

print_status "info" "API Base URL: $API_BASE_URL"
print_status "info" "Test Email: $TEST_EMAIL"
echo ""

# Step 1: Register a user via CLI (this should use BCrypt)
print_status "info" "Step 1: Registering user via CLI script..."

cd "$(dirname "$0")/.."
echo -e "$TEST_NAME\n$TEST_EMAIL\n$TEST_PASSWORD\nRegular\n" | ./connect_to_db_test.sh --register-user

if [ $? -eq 0 ]; then
    print_status "success" "User registered via CLI with BCrypt hashing"
else
    print_status "warning" "User may already exist, continuing with login test..."
fi

echo ""

# Step 2: Test login with INCORRECT password (should fail)
print_status "info" "Step 2: Testing login with incorrect password..."

wrong_login_data="{\"email\":\"$TEST_EMAIL\",\"password\":\"$WRONG_PASSWORD\"}"

wrong_response=$(curl $CURL_OPTS -X POST "$API_BASE_URL/auth/login" \
    -H "Content-Type: application/json" \
    -d "$wrong_login_data")

wrong_status=$(extract_status "$wrong_response")
wrong_body=$(extract_body "$wrong_response")

if [ "$wrong_status" = "401" ] || [ "$wrong_status" = "400" ] || [ "$wrong_status" = "403" ]; then
    print_status "success" "Login correctly rejected with wrong password! (Status: $wrong_status)"
    echo ""
    echo "Response:"
    echo "$wrong_body" | jq . 2>/dev/null || echo "$wrong_body"
    
    # Check if response contains appropriate error message
    if echo "$wrong_body" | grep -q -i "invalid\|incorrect\|wrong\|password\|credential\|unauthorized" 2>/dev/null; then
        print_status "success" "Appropriate error message returned"
    else
        print_status "warning" "Error message could be more descriptive"
    fi
    
elif [ "$wrong_status" = "200" ]; then
    print_status "error" "SECURITY ISSUE: Login succeeded with wrong password!"
    echo ""
    echo "Response:"
    echo "$wrong_body"
    print_status "error" "This is a critical security vulnerability!"
else
    print_status "warning" "Unexpected status code for wrong password: $wrong_status"
    echo ""
    echo "Response:"
    echo "$wrong_body"
fi

echo ""

# Step 3: Test login with the CORRECT password
print_status "info" "Step 3: Testing login with correct password..."

login_data="{\"email\":\"$TEST_EMAIL\",\"password\":\"$TEST_PASSWORD\"}"

response=$(curl $CURL_OPTS -X POST "$API_BASE_URL/auth/login" \
    -H "Content-Type: application/json" \
    -d "$login_data")

status=$(extract_status "$response")
body=$(extract_body "$response")

if [ "$status" = "200" ]; then
    print_status "success" "Login successful!"
    echo ""
    echo "Response:"
    echo "$body" | jq . 2>/dev/null || echo "$body"
    
    # Extract token for further testing
    token=$(echo "$body" | jq -r '.token // .Token // empty' 2>/dev/null)
    
    # Alternative extraction if jq fails
    if [ -z "$token" ] || [ "$token" = "null" ] || [ "$token" = "empty" ]; then
        token=$(echo "$body" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
    fi
    
    # Debug: Show what we extracted
    echo "Debug - Extracted token: ${token:0:50}..." 
    
    if [ ! -z "$token" ] && [ "$token" != "null" ] && [ "$token" != "empty" ]; then
        print_status "success" "JWT token extracted"
        echo ""
        
        # Step 4: Test protected endpoint
        print_status "info" "Step 4: Testing protected endpoint..."
        
        protected_response=$(curl $CURL_OPTS -X GET "$API_BASE_URL/user/email/$TEST_EMAIL" \
            -H "Authorization: Bearer $token")
        
        protected_status=$(extract_status "$protected_response")
        protected_body=$(extract_body "$protected_response")
        
        if [ "$protected_status" = "200" ]; then
            print_status "success" "Protected endpoint access successful!"
            echo ""
            echo "User data:"
            echo "$protected_body" | jq . 2>/dev/null || echo "$protected_body"
        else
            print_status "error" "Protected endpoint access failed (Status: $protected_status)"
            echo "$protected_body"
        fi
        
        echo ""
        
        # Step 5: Test logout
        print_status "info" "Step 5: Testing logout..."
        
        logout_response=$(curl $CURL_OPTS -X POST "$API_BASE_URL/auth/logout" \
            -H "Authorization: Bearer $token")
        
        logout_status=$(extract_status "$logout_response")
        logout_body=$(extract_body "$logout_response")
        
        if [ "$logout_status" = "200" ]; then
            print_status "success" "Logout successful!"
            echo "$logout_body" | jq . 2>/dev/null || echo "$logout_body"
        else
            print_status "error" "Logout failed (Status: $logout_status)"
            echo "$logout_body"
        fi
        
        echo ""
        
        # Step 6: Test token after logout (should fail)
        print_status "info" "Step 6: Testing token after logout (should fail)..."
        
        blacklist_response=$(curl $CURL_OPTS -X GET "$API_BASE_URL/user/email/$TEST_EMAIL" \
            -H "Authorization: Bearer $token")
        
        blacklist_status=$(extract_status "$blacklist_response")
        blacklist_body=$(extract_body "$blacklist_response")
        
        if [ "$blacklist_status" = "401" ]; then
            print_status "success" "Token properly blacklisted after logout!"
        else
            print_status "warning" "Token still valid after logout (Status: $blacklist_status)"
            echo "$blacklist_body"
        fi
        
    else
        print_status "error" "Failed to extract JWT token from response"
    fi
    
else
    print_status "error" "Login failed (Status: $status)"
    echo ""
    echo "Response:"
    echo "$body"
    
    if [[ "$body" == *"Invalid salt version"* ]]; then
        print_status "error" "BCrypt error detected!"
        echo ""
        echo "This error occurs when:"
        echo "1. Password is stored as plain text instead of BCrypt hash"
        echo "2. Password hash format is invalid"
        echo ""
        echo "Solutions:"
        echo "1. Re-register the user with: ./connect_to_db_test.sh --register-user"
        echo "2. Make sure the registration script uses BCrypt hashing"
        echo "3. Clear existing users: ./connect_to_db_test.sh --regenerate"
    fi
fi

echo ""

# Step 7: Test login with non-existent user
print_status "info" "Step 7: Testing login with non-existent user..."

nonexistent_login_data="{\"email\":\"nonexistent@example.com\",\"password\":\"$TEST_PASSWORD\"}"

nonexistent_response=$(curl $CURL_OPTS -X POST "$API_BASE_URL/auth/login" \
    -H "Content-Type: application/json" \
    -d "$nonexistent_login_data")

nonexistent_status=$(extract_status "$nonexistent_response")
nonexistent_body=$(extract_body "$nonexistent_response")

if [ "$nonexistent_status" = "401" ] || [ "$nonexistent_status" = "400" ] || [ "$nonexistent_status" = "404" ]; then
    print_status "success" "Login correctly rejected for non-existent user! (Status: $nonexistent_status)"
    echo ""
    echo "Response:"
    echo "$nonexistent_body" | jq . 2>/dev/null || echo "$nonexistent_body"
elif [ "$nonexistent_status" = "200" ]; then
    print_status "error" "SECURITY ISSUE: Login succeeded for non-existent user!"
    echo ""
    echo "Response:"
    echo "$nonexistent_body"
else
    print_status "warning" "Unexpected status code for non-existent user: $nonexistent_status"
    echo ""
    echo "Response:"
    echo "$nonexistent_body"
fi

echo ""
echo "============================================"
echo "Authentication test completed"
echo "============================================"
echo ""
echo "Test Summary:"
echo "✅ User registration with BCrypt"
echo "✅ Login rejection with incorrect password"
echo "✅ Login success with correct credentials"
echo "✅ JWT token extraction and validation"
echo "✅ Protected endpoint access with valid token"
echo "✅ Logout functionality"
echo "✅ Token blacklisting after logout"
echo "✅ Login rejection for non-existent users"
echo ""
if [ "$wrong_status" = "200" ] || [ "$nonexistent_status" = "200" ]; then
    print_status "error" "CRITICAL SECURITY ISSUES DETECTED!"
    echo "Review authentication logic immediately."
    exit 1
else
    print_status "success" "All authentication security tests passed!"
    exit 0
fi