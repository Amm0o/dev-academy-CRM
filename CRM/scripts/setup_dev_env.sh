#!/bin/bash

# Function to check if running as root
check_root() {
    if [ "$EUID" -ne 0 ]; then
        echo "This script needs to be ran as root"
        exit 1
    fi
}

# Detect OS
if [ -f /etc/os-release ]; then
    . /etc/os-release
    OS=$ID
else
    echo "Cannot detect OS. Exiting."
    exit 1
fi

# Function to install GIT
installing_git() {
    echo "Installing git..."
    sleep 1
    if [ "$OS" = "arch" ]; then
        if pacman -Qi "git" &> /dev/null; then
            echo "Git is already installed... proceeding to next step"
            return 0
        fi
        pacman -Sy
        pacman -S --noconfirm git
    elif [ "$OS" = "ubuntu" ]; then
        if dpkg -s git &> /dev/null; then
            echo "Git is already installed... proceeding to next step"
            return 0
        fi
        apt update
        apt install -y git
    else
        echo "Unsupported OS: $OS. Please install git manually."
        exit 1
    fi
    if [ $? -eq 0 ]; then
        echo "Git installed"
        version_git=$(git --version)
        printf "Git version $version_git"
    else
        echo "Failed to install git code $?"
        exit 1
    fi
}

install_dotnet() {
    echo "Installing .Net 8.0"
    if [ "$OS" = "arch" ]; then
        pacman -S --noconfirm dotnet-sdk-8.0
        pacman -S --noconfirm aspnet-runtime-8.0
    elif [ "$OS" = "ubuntu" ]; then
        wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
        dpkg -i packages-microsoft-prod.deb
        rm packages-microsoft-prod.deb
        apt update
        apt install -y dotnet-sdk-8.0 aspnetcore-runtime-8.0
    else
        echo "Unsupported OS: $OS. Please install .NET 8.0 manually."
        exit 1
    fi
    check_if_installed ".Net 8.0"
}

install_dotnet_runtime() {
    echo "Installing .Net runtime 8.0"
    if [ "$OS" = "arch" ]; then
        pacman -S --noconfirm aspnet-runtime-8.0
    elif [ "$OS" = "ubuntu" ]; then
        apt install -y aspnetcore-runtime-8.0
    else
        echo "Unsupported OS: $OS. Please install .NET runtime manually."
        exit 1
    fi
    check_if_installed "aspnet-runtime-8.0"
}

# Logger function to check if something is installed
check_if_installed() {
    echo "Checking if $1 is installed..."
    # For .NET, check dotnet version
    if command -v dotnet &> /dev/null; then
        echo "$1 is installed"
    else
        echo "$1 is not installed"
        exit 1
    fi
}

printf "This is the setup script for the CRM project.\nSupported OS: Arch Linux, Ubuntu.\n"
sleep 2

echo  -n "Do you want to continue? (y/N)"
read answer

if [[ "$answer" == "y" || "$answer" == "Y" ]]; then
    echo "Proceeding with installation..."
    sleep 1
    check_root
    installing_git
    install_dotnet
    install_dotnet_runtime
else
    echo "exiting installation"
    exit 1
fi
