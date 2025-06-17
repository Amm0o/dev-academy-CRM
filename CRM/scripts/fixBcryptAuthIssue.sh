#!/bin/bash

# Quick Fix Script for BCrypt Authentication Issue
# This script clears existing users and re-registers them with proper BCrypt hashing

# Configuration
SERVER="localhost,1433"
USER="SA"
PASSWORD="YourStrong@Passw0rd123!"
DATABASE="CRM"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

print_status() {
    case $1 in
        "success") echo -e "${GREEN}✓ $2${NC}" ;;
        "error") echo -e "${RED}✗ $2${NC}" ;;
        "info") echo -e "${YELLOW}ℹ $2${NC}" ;;
    esac
}

echo "BCrypt Authentication Fix"
echo "========================"
echo ""

# Check if BCrypt is available
if ! python3 -c "import bcrypt" 2>/dev/null; then
    print_status "error" "BCrypt not available. Installing..."
    pip3 install bcrypt
    if [ $? -ne 0 ]; then
        print_status "error" "Failed to install BCrypt. Run: pip3 install bcrypt"
        exit 1
    fi
fi

print_status "success" "BCrypt is available"

# Step 1: Clear existing users with plain text passwords
print_status "info" "Clearing existing users..."

sqlcmd -S "$SERVER" -U "$USER" -P "$PASSWORD" -C <<EOF
USE $DATABASE;
GO

-- Delete all existing users (they have plain text passwords)
DELETE FROM Users;
PRINT 'All existing users deleted';

GO
EOF

if [ $? -eq 0 ]; then
    print_status "success" "Existing users cleared"
else
    print_status "error" "Failed to clear users"
    exit 1
fi

# Step 2: Register test user with BCrypt
print_status "info" "Registering test user with BCrypt..."

TEST_NAME="Test User"
TEST_EMAIL="testuser@example.com"
TEST_PASSWORD="StrongPassword123!"

# Generate BCrypt hash
password_hash=$(python3 << EOF
import bcrypt
password = '$TEST_PASSWORD'.encode('utf-8')
salt = bcrypt.gensalt()
hashed = bcrypt.hashpw(password, salt)
print(hashed.decode('utf-8'))
EOF
)

if [ $? -ne 0 ]; then
    print_status "error" "Failed to generate BCrypt hash"
    exit 1
fi

# Insert user with BCrypt hash
sqlcmd -S "$SERVER" -U "$USER" -P "$PASSWORD" -C <<EOF
USE $DATABASE;
GO

INSERT INTO Users (Name, Email, PasswordHash, Role, UserCreateTime, UserUpdateTime)
VALUES (
    '$TEST_NAME',
    '$TEST_EMAIL',
    '$password_hash',
    'Regular',
    GETUTCDATE(),
    GETUTCDATE()
);

PRINT 'Test user registered with BCrypt hash';
SELECT UserId, Name, Email, Role FROM Users WHERE Email = '$TEST_EMAIL';

GO
EOF

if [ $? -eq 0 ]; then
    print_status "success" "Test user registered successfully"
else
    print_status "error" "Failed to register test user"
    exit 1
fi

echo ""
print_status "success" "BCrypt fix completed!"
echo ""
echo "You can now test authentication with:"
echo "  Email: $TEST_EMAIL"
echo "  Password: $TEST_PASSWORD"
echo ""
echo "Run the test: ./scripts/tests/testAuth.sh"