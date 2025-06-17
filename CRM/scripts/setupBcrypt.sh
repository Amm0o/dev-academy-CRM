#!/bin/bash

# BCrypt Installation Helper Script
echo "CRM BCrypt Installation Helper"
echo "============================="

# Check if python3 is installed
if ! command -v python3 &> /dev/null; then
    echo "❌ Python3 is not installed"
    echo ""
    echo "Installation instructions:"
    echo "  Ubuntu/Debian: sudo apt install python3 python3-pip"
    echo "  Arch Linux: sudo pacman -S python python-pip"
    echo "  CentOS/RHEL: sudo yum install python3 python3-pip"
    echo ""
    exit 1
else
    echo "✅ Python3 is installed: $(python3 --version)"
fi

# Check if pip3 is available
if ! command -v pip3 &> /dev/null; then
    echo "❌ pip3 is not installed"
    echo ""
    echo "Installation instructions:"
    echo "  Ubuntu/Debian: sudo apt install python3-pip"
    echo "  Arch Linux: sudo pacman -S python-pip"
    echo "  CentOS/RHEL: sudo yum install python3-pip"
    echo ""
    exit 1
else
    echo "✅ pip3 is available: $(pip3 --version)"
fi

# Check if bcrypt is already installed
python3 -c "import bcrypt; print('✅ bcrypt is already installed:', bcrypt.__version__)" 2>/dev/null

if [ $? -eq 0 ]; then
    echo "✅ BCrypt is ready to use!"
    exit 0
fi

echo ""
echo "Installing bcrypt library..."

# Try to install bcrypt
pip3 install bcrypt

if [ $? -eq 0 ]; then
    echo "✅ BCrypt installed successfully!"
    
    # Test the installation
    python3 -c "
import bcrypt
password = b'test'
salt = bcrypt.gensalt()
hashed = bcrypt.hashpw(password, salt)
print('✅ BCrypt test successful!')
print('Generated hash:', hashed.decode('utf-8'))
"
    
    echo ""
    echo "🎉 BCrypt is now ready for use with the CRM application!"
    echo ""
    echo "You can now register users with:"
    echo "  ./connect_to_db_test.sh --register-user"
    
else
    echo "❌ Failed to install bcrypt"
    echo ""
    echo "Try manual installation:"
    echo "  pip3 install --user bcrypt"
    echo ""
    echo "Or with sudo:"
    echo "  sudo pip3 install bcrypt"
    exit 1
fi