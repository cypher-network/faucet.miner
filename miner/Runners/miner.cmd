@echo off
pushd %USERPROFILE%\.miner & dotnet miner.dll %* & popd
