#!/bin/bash

printf "This is the setup script for the CRM project in arch linux \nif you are installing in other distros like ubuntu do it at your own risk.\n"
sleep 2

echo  -n "Do you want to continue? (y/N)"
read answer

if [[ "$answer" == "y" || "$answer" == "Y"]]; then
    echo "Proceeding with installation..."
    sleep 1
else
    echo "exiting installation"
    exit 1
fi
