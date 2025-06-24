#!/usr/bin/env bash 
git pull
git submodule update --init --recursive
git read-tree -mu HEAD
echo "Press ENTER to exit."
read
