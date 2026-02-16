tdr -S  --indent-size=8 --no_comm_files ../xml/keywords_Res.xml -O ../include/

tdr -S  --indent-size=8 --no_comm_files ../xml/Ds_Res.xml -O ../include/

tdr -S  --indent-size=8 --no_comm_files ../xml/Ds_Config.xml -O ../include/

tdr -S  --indent-size=8 --no_comm_files ../xml/CSBattleProtocol.xml -O ../include/ ../xml/CSBattleComm.xml

xcopy ..\xml\*.cs ..\include /y

lua52 "chunk_res.lua"

python xml2c#header_export.py

xcopy ..\include\Ds_Res.cs ..\..\BattleCore\Script\Protocol /y
xcopy ..\include\keywords_Res.cs ..\..\BattleCore\Script\Protocol /y
xcopy ..\include\Ds_Config.cs ..\..\BattleCore\Script\Protocol /y
xcopy ..\include\*_macros.cs ..\..\BattleCore\Script\Protocol /y
xcopy ..\include\*_metalib.cs ..\..\BattleCore\Script\Protocol /y
xcopy ..\include\CSBattleComm.cs ..\..\BattleCore\Script\Protocol /y
xcopy ..\include\CSBattleProtocol.cs ..\..\BattleCore\Script\Protocol /y
xcopy ..\include\Ds_ResExport.cs ..\..\BattleCore\Script\Protocol /y
pause
