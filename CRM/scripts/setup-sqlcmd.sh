if ! [ -x "$(command -v sqlcmd)" ]; then
    echo "Installing mssql-tools (sqlcmd)..."

    mkdir -p /tmp/aur_build && cd /tmp/aur_build
    git clone https://aur.archlinux.org/msodbcsql.git
    chown -R anoliveira:anoliveira /tmp/aur_build
    cd msodbcsql
    sudo -u anoliveira makepkg

    # Install the built package
    sudo pacman -U --noconfirm msodbcsql-*.pkg.tar.zst
    
    # Create build directory and set permissions
    chmod 777 /tmp/aur_build
    cd /tmp/aur_build
    
    # Install dependencies
    pacman -S --noconfirm base-devel git
    
    # Clone the mssql-tools AUR package
    git clone https://aur.archlinux.org/mssql-tools.git
    chown -R anoliveira:anoliveira /tmp/aur_build
    
    # Build and install the package as non-root user
    cd mssql-tools
    sudo -u anoliveira makepkg -s
    
    # Install the built package
    pacman -U --noconfirm mssql-tools-*.pkg.tar.zst
    
    # Add to PATH
    echo 'export PATH="$PATH:/opt/mssql-tools/bin"' >> /etc/profile.d/mssql-tools.sh
    source /etc/profile.d/mssql-tools.sh
    
    # Cleanup
    cd /
    rm -rf /tmp/aur_build
else
    echo "sqlcmd is already installed."
fi