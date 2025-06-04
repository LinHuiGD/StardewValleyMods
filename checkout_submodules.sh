#!/usr/bin/env bash
echo "Common" > .git/modules/External/Pathoschild/StardewMods/info/sparse-checkout
echo "Common.Patching" >> .git/modules/External/Pathoschild/StardewMods/info/sparse-checkout 
cd External/Pathoschild/StardewMods
git config core.sparsecheckout true
git checkout
cd ../../..
echo "Press ENTER to exit."
read
