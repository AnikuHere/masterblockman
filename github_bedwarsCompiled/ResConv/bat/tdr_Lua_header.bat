cd ./

python chunk_Lua.py

if exist ..\xml\Ds_keywords.lua xcopy/Y ..\xml\Ds_keywords.lua ..\..\..\blockunity-server\logicserver\lualib\res\
if exist ..\xml\Ds_CSBattleComm.lua xcopy/Y ..\xml\Ds_CSBattleComm.lua ..\..\..\blockunity-server\logicserver\lualib\res\

pause



