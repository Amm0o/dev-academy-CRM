#!/bin/bash




# Function to check if running as root
check_root() {
    if [ "$EUID" -ne 0 ]; then
        echo "This script needs to be ran as root"
        return 1
    fi
}

# Function to install GIT
installing_git() {
    echo "Installing git..."
    sleep 1

    # Check if package is installed
    if pacman -Qi "git" &> /dev/null; then
        echo "Git is already installed... proceeding to next step"
        return 0
    fi

    # Attempt to install package
    echo "Installing GIT since it's not installed already"
    # Update repos
    pacman -Sy
    # Now install git
    pacman -S --noconfirm git

    if [ $? -eq 0 ]; then
        echo "Git installed"
        version_git=$(git --version)
        printf "Git version $version_git"
    else
        echo "Failed to install git code $?"
        return 1
    fi

}

install_dotnet() {

    echo "Installing .Net 8.0"
    # Intall the .Net SDK
    pacman -S dotnet-sdk-8.0
    check_if_installed ".Net 8.0"

}

install_dotnet_runtime() {
    echo "Installing .Net runtime 8.0"

    check_if_installed "aspnet-runtime-8.0"
    if [ ! $? -eq 0 ]; then
        echo "aspnet-runtime-8.0 is not installed"
        pacman -S aspnet-runtime-8.0

    fi
}

# Logger function to check is something installed
check_if_installed() {
    echo "Checking if $1 is installed..."
    if [ $? -eq 0 ]; then
        echo "$1 is installed"
    else 
      echo "$1 is not installed"
      return 1
    fi
}

printf "This is the setup script for the CRM project in arch linux \nif you are installing in other distros like ubuntu it will not work.\n"
sleep 2

echo  -n "Do you want to continue? (y/N)"
read answer

if [[ "$answer" == "y" || "$answer" == "Y" ]]; then
    echo "Proceeding with installation..."
    sleep 1
    # start calling functions
    check_root
    installing_git
    install_dotnet
    install_dotnet_runtime
else
    echo "exiting installation"
    exit 1
fi
