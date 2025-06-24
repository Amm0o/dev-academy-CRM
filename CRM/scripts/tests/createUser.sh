#!/bin/bash

API_URL="http://localhost:5205/api/User/register"

# Example user data
read -r -d '' USER_JSON <<EOF
{
  "Name": "Test User",
  "Email": "testuser@example.com",
  "Password": "StrongPassword123!"
  "Role": "Admin"
}
EOF

echo "Sending user registration request to $API_URL"
curl -i -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -d "$USER_JSON"