#!/usr/bin/env bash 
git submodule update --remote --recursive --checkout
git read-tree -mu HEAD
echo "Press ENTER to exit."
read
