#!/bin/bash

if ! [ -x "$(command -v sqlcmd)" ]; then
    echo "Installing mssql-tools (sqlcmd)..."

    # Detect OS
    if [ -f /etc/arch-release ]; then
        # Arch Linux installation
        sudo pacman -S --noconfirm base-devel git
        mkdir -p /tmp/aur_build && cd /tmp/aur_build
        git clone https://aur.archlinux.org/msodbcsql.git
        chown -R $(whoami):$(whoami) /tmp/aur_build
        cd msodbcsql
        sudo -u $(whoami) makepkg -s
        sudo pacman -U --noconfirm msodbcsql-*.pkg.tar.zst
        cd ..
        git clone https://aur.archlinux.org/mssql-tools.git
        chown -R $(whoami):$(whoami) mssql-tools
        cd mssql-tools
        sudo -u $(whoami) makepkg -s
        sudo pacman -U --noconfirm mssql-tools-*.pkg.tar.zst
        echo 'export PATH="$PATH:/opt/mssql-tools/bin"' | sudo tee /etc/profile.d/mssql-tools.sh
        source /etc/profile.d/mssql-tools.sh
        cd /
        rm -rf /tmp/aur_build
    elif [ -f /etc/lsb-release ] || [ -f /etc/debian_version ]; then
        # Ubuntu/Debian installation
        sudo apt-get update
        sudo apt-get install -y curl apt-transport-https software-properties-common
        curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
        UBUNTU_VERSION=$(lsb_release -rs)
        curl https://packages.microsoft.com/config/ubuntu/$UBUNTU_VERSION/prod.list | sudo tee /etc/apt/sources.list.d/mssql-release.list
        sudo apt-get update
        sudo ACCEPT_EULA=Y apt-get install -y msodbcsql18 mssql-tools18 unixodbc-dev
        echo 'export PATH="$PATH:/opt/mssql-tools18/bin"' >> ~/.bashrc
        source ~/.bashrc
    else
        echo "Unsupported OS. Please install sqlcmd manually."
        exit 1
    fi
else
    echo "sqlcmd is already installed."
fi